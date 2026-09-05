using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Trailblazers.Backend.Core.Application.Features.Exams.GetExamMetadata;
using Trailblazers.Backend.Core.Application.Interfaces;
using Trailblazers.Backend.Core.Application.Submissions.Commands;
using Trailblazers.Backend.Core.Application.Submissions.Queries;
using Trailblazers.Backend.Core.Domain.Entities;
using Trailblazers.Backend.Core.Domain.Repositories;
using Trailblazers.Backend.Infrastructure.Persistence;
using Trailblazers.Backend.Infrastructure.Persistence.Repositories;
using Trailblazers.Backend.Infrastructure.Services;
using Trailblazers.Backend.WebApi.Authentication;

DotNetEnv.Env.Load();

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// PostgreSQL Connection String
var connectionString = Environment.GetEnvironmentVariable("CONNECTION_STRING")
                       ?? builder.Configuration.GetConnectionString("DefaultConnection")
                       ?? "Host=localhost;Database=trailblazers_db;Username=postgres;Password=postgres";

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString,
        npgsqlOptions =>
        {
            npgsqlOptions.EnableRetryOnFailure(
                maxRetryCount: 3,
                maxRetryDelay: TimeSpan.FromSeconds(5),
                errorCodesToAdd: null);
        }));

// ASP.NET Core Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
{
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequiredLength = 6;
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// JWT Authentication
var jwtKey = Environment.GetEnvironmentVariable("JWT_SECRET")
             ?? builder.Configuration["JWT_SECRET"]
             ?? "trailblazers-super-secure-jwt-key-2026-min-32-bytes";
var jwtIssuer = Environment.GetEnvironmentVariable("JWT_ISSUER")
               ?? builder.Configuration["JWT_ISSUER"]
               ?? "trailblazers-api";
var jwtAudience = Environment.GetEnvironmentVariable("JWT_AUDIENCE")
                 ?? builder.Configuration["JWT_AUDIENCE"]
                 ?? "trailblazers-client";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ValidateIssuer = true,
        ValidIssuer = jwtIssuer,
        ValidateAudience = true,
        ValidAudience = jwtAudience,
        ClockSkew = TimeSpan.Zero
    };
});

// Register CORS
var allowedOriginsEnv = Environment.GetEnvironmentVariable("ALLOWED_ORIGINS");
var allowedOrigins = !string.IsNullOrWhiteSpace(allowedOriginsEnv)
    ? allowedOriginsEnv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
    : ["http://localhost:3000"];

builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", policy =>
    {
        policy.WithOrigins(allowedOrigins)
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

// MediatR Registration
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);
    cfg.RegisterServicesFromAssembly(typeof(GetExamMetadataQuery).Assembly);
});

// Dependency Injection
builder.Services.AddHttpClient<IJambApiService, RapidApiJambService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(500);
});
builder.Services.AddScoped<ISubmissionRepository, SubmissionRepository>();
builder.Services.AddScoped<IExamQuestionRepository, ExamQuestionRepository>();
builder.Services.AddScoped<IExamSessionRepository, ExamSessionRepository>();
builder.Services.AddScoped<IMailService, MailService>();
builder.Services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();
builder.Services.AddHostedService<QueuedHostedService>();
builder.Services.AddScoped<SubmitContactCommandHandler>();
builder.Services.AddScoped<SubmitRegistrationCommandHandler>();
builder.Services.AddScoped<GetSubmissionsQueryHandler>();
builder.Services.AddScoped<ApiKeyAuthFilter>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IStudentStatusService, StudentStatusService>();

builder.Services.AddHealthChecks()
    .AddDbContextCheck<ApplicationDbContext>(
        customTestQuery: async (context, cancellationToken) =>
        {
            await RelationalDatabaseFacadeExtensions.ExecuteSqlRawAsync(context.Database, "SELECT 1",
                cancellationToken);
            return true;
        });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors("CorsPolicy");

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapHealthChecks("/api/health");

// Apply migrations / ensure database exists on startup and seed roles / demo accounts
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        context.Database.Migrate();
        app.Logger.LogInformation("Database migrations applied successfully.");

        var roleManager = services.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

        string[] roles = ["Admin", "Instructor", "Student"];
        foreach (var role in roles)
        {
            if (!roleManager.RoleExistsAsync(role).GetAwaiter().GetResult())
            {
                roleManager.CreateAsync(new IdentityRole<Guid>(role)).GetAwaiter().GetResult();
                app.Logger.LogInformation("Role '{Role}' seeded.", role);
            }
        }

        // Seed demo accounts if missing
        var seedUsers = new[]
        {
            new { Email = "admin@trailblazer.edu", Name = "System Administrator", Role = "Admin", Password = "AdminPassword123!" },
            new { Email = "instructor@trailblazer.edu", Name = "Lead Instructor", Role = "Instructor", Password = "Instructor123!" },
            new { Email = "student@trailblazer.edu", Name = "Demo Student", Role = "Student", Password = "Student123!" }
        };

        foreach (var su in seedUsers)
        {
            var user = userManager.FindByEmailAsync(su.Email).GetAwaiter().GetResult();
            if (user == null)
            {
                var newUser = new ApplicationUser
                {
                    UserName = su.Email,
                    Email = su.Email,
                    FullName = su.Name,
                    IsActive = true,
                    CreatedAt = DateTimeOffset.UtcNow
                };

                var createRes = userManager.CreateAsync(newUser, su.Password).GetAwaiter().GetResult();
                if (createRes.Succeeded)
                {
                    userManager.AddToRoleAsync(newUser, su.Role).GetAwaiter().GetResult();
                    app.Logger.LogInformation("User '{Email}' created with role '{Role}'.", su.Email, su.Role);
                }
                else
                {
                    app.Logger.LogWarning("Failed to seed user '{Email}': {Errors}", su.Email, string.Join(", ", createRes.Errors.Select(e => e.Description)));
                }
            }
        }
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "An error occurred while initializing the database.");
    }
}

app.Run();
