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
using Trailblazers.Backend.Core.Application.Features.GuardianPortal.Interfaces;
using Trailblazers.Backend.Core.Application.Features.Blog.Interfaces;
using Trailblazers.Backend.WebApi.Authentication;

DotNetEnv.Env.Load();

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });
builder.Services.AddHttpContextAccessor();
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
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            // If Authorization header is absent, look for HTTP-Only cookie 'auth_token'
            if (string.IsNullOrEmpty(context.Token) && context.Request.Cookies.TryGetValue("auth_token", out var cookieToken))
            {
                context.Token = cookieToken;
            }
            return Task.CompletedTask;
        }
    };
});

// Register CORS for multi-subdomain support
builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", policy =>
    {
        policy.SetIsOriginAllowed(origin =>
        {
            if (string.IsNullOrWhiteSpace(origin)) return false;
            try
            {
                var uri = new Uri(origin);
                var host = uri.Host.ToLowerInvariant();
                return host == "localhost"
                    || host.EndsWith(".localhost")
                    || host == "trailblazer-academy.com"
                    || host.EndsWith(".trailblazer-academy.com");
            }
            catch
            {
                return false;
            }
        })
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
builder.Services.AddHttpClient();
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
builder.Services.AddScoped<IAttendanceService, AttendanceService>();
builder.Services.AddScoped<IEmailTemplateService, EmailTemplateService>();
builder.Services.AddScoped<IStaffInvitationService, StaffInvitationService>();
builder.Services.AddScoped<IStaffManagementService, StaffManagementService>();
builder.Services.AddScoped<IGuardianReportService, GuardianReportService>();
builder.Services.AddScoped<IAnnouncementService, AnnouncementService>();
builder.Services.AddScoped<IGuardianPortalService, GuardianPortalService>();
builder.Services.AddScoped<IImageStorageService, CloudinaryService>();
builder.Services.AddScoped<IBlogService, BlogService>();

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
        try
        {
            context.Database.Migrate();
            app.Logger.LogInformation("Database migrations applied successfully.");
        }
        catch (Exception migEx)
        {
            app.Logger.LogWarning(migEx, "Database.Migrate() warning (continuing to safety patch)");
        }

        // Idempotent schema safety patch: Ensure clock_out columns and announcements table exist unconditionally
        try
        {
            context.Database.ExecuteSqlRaw(@"
                -- 1. Ensure attendance_records clock-out columns exist
                ALTER TABLE IF EXISTS attendance_records ADD COLUMN IF NOT EXISTS clock_out_accuracy_meters double precision;
                ALTER TABLE IF EXISTS attendance_records ADD COLUMN IF NOT EXISTS clock_out_distance_meters double precision;
                ALTER TABLE IF EXISTS attendance_records ADD COLUMN IF NOT EXISTS clock_out_latitude double precision;
                ALTER TABLE IF EXISTS attendance_records ADD COLUMN IF NOT EXISTS clock_out_longitude double precision;
                ALTER TABLE IF EXISTS attendance_records ADD COLUMN IF NOT EXISTS clock_out_time timestamp with time zone;

                -- 2. Ensure announcements table exists for Noticeboard
                CREATE TABLE IF NOT EXISTS announcements (
                    ""Id"" uuid NOT NULL PRIMARY KEY,
                    ""Title"" character varying(250) NOT NULL,
                    ""Content"" text NOT NULL,
                    ""Priority"" character varying(50) NOT NULL,
                    ""TargetAudience"" character varying(50) NOT NULL DEFAULT 'All',
                    ""IsActive"" boolean NOT NULL DEFAULT true,
                    ""CreatedAt"" timestamp with time zone NOT NULL,
                    ""ExpiresAt"" timestamp with time zone NULL,
                    ""AuthorName"" character varying(200) NOT NULL,
                    ""AuthorId"" uuid NULL,
                    ""SentEmailBroadcast"" boolean NOT NULL DEFAULT false,
                    ""SentSmsBroadcast"" boolean NOT NULL DEFAULT false
                );
                ALTER TABLE IF EXISTS announcements ADD COLUMN IF NOT EXISTS ""Id"" uuid;
                ALTER TABLE IF EXISTS announcements ADD COLUMN IF NOT EXISTS ""Title"" character varying(250);
                ALTER TABLE IF EXISTS announcements ADD COLUMN IF NOT EXISTS ""Content"" text;
                ALTER TABLE IF EXISTS announcements ADD COLUMN IF NOT EXISTS ""Priority"" character varying(50);
                ALTER TABLE IF EXISTS announcements ADD COLUMN IF NOT EXISTS ""TargetAudience"" character varying(50) DEFAULT 'All';
                ALTER TABLE IF EXISTS announcements ADD COLUMN IF NOT EXISTS ""IsActive"" boolean DEFAULT true;
                ALTER TABLE IF EXISTS announcements ADD COLUMN IF NOT EXISTS ""CreatedAt"" timestamp with time zone;
                ALTER TABLE IF EXISTS announcements ADD COLUMN IF NOT EXISTS ""ExpiresAt"" timestamp with time zone;
                ALTER TABLE IF EXISTS announcements ADD COLUMN IF NOT EXISTS ""AuthorName"" character varying(200);
                ALTER TABLE IF EXISTS announcements ADD COLUMN IF NOT EXISTS ""AuthorId"" uuid;
                ALTER TABLE IF EXISTS announcements ADD COLUMN IF NOT EXISTS ""SentEmailBroadcast"" boolean DEFAULT false;
                ALTER TABLE IF EXISTS announcements ADD COLUMN IF NOT EXISTS ""SentSmsBroadcast"" boolean DEFAULT false;
                CREATE INDEX IF NOT EXISTS ""IX_announcements_CreatedAt"" ON announcements (""CreatedAt"");
                CREATE INDEX IF NOT EXISTS ""IX_announcements_IsActive"" ON announcements (""IsActive"");

                -- 3. Ensure student_notifications table exists
                CREATE TABLE IF NOT EXISTS student_notifications (
                    ""Id"" uuid NOT NULL PRIMARY KEY,
                    ""StudentId"" uuid NOT NULL,
                    ""StudentEmail"" character varying(256) NOT NULL,
                    ""Title"" character varying(200) NOT NULL,
                    ""Message"" character varying(2000) NOT NULL,
                    ""Type"" character varying(50) NOT NULL DEFAULT 'General',
                    ""IsRead"" boolean NOT NULL DEFAULT false,
                    ""CreatedAt"" timestamp with time zone NOT NULL,
                    ""CreatedBy"" character varying(200) NULL
                );
                ALTER TABLE IF EXISTS student_notifications ADD COLUMN IF NOT EXISTS ""Id"" uuid;
                ALTER TABLE IF EXISTS student_notifications ADD COLUMN IF NOT EXISTS ""StudentId"" uuid;
                ALTER TABLE IF EXISTS student_notifications ADD COLUMN IF NOT EXISTS ""StudentEmail"" character varying(256);
                ALTER TABLE IF EXISTS student_notifications ADD COLUMN IF NOT EXISTS ""Title"" character varying(200);
                ALTER TABLE IF EXISTS student_notifications ADD COLUMN IF NOT EXISTS ""Message"" character varying(2000);
                ALTER TABLE IF EXISTS student_notifications ADD COLUMN IF NOT EXISTS ""Type"" character varying(50) DEFAULT 'General';
                ALTER TABLE IF EXISTS student_notifications ADD COLUMN IF NOT EXISTS ""IsRead"" boolean DEFAULT false;
                ALTER TABLE IF EXISTS student_notifications ADD COLUMN IF NOT EXISTS ""CreatedAt"" timestamp with time zone;
                ALTER TABLE IF EXISTS student_notifications ADD COLUMN IF NOT EXISTS ""CreatedBy"" character varying(200);
                CREATE INDEX IF NOT EXISTS ""IX_student_notifications_StudentId"" ON student_notifications (""StudentId"");
                CREATE INDEX IF NOT EXISTS ""IX_student_notifications_StudentEmail"" ON student_notifications (""StudentEmail"");
                CREATE INDEX IF NOT EXISTS ""IX_student_notifications_CreatedAt"" ON student_notifications (""CreatedAt"");
                CREATE INDEX IF NOT EXISTS ""IX_student_notifications_IsRead"" ON student_notifications (""IsRead"");

                -- 4. Ensure blog_posts table exists
                CREATE TABLE IF NOT EXISTS blog_posts (
                    ""Id"" uuid NOT NULL PRIMARY KEY,
                    ""Title"" character varying(250) NOT NULL,
                    ""Slug"" character varying(250) NOT NULL,
                    ""Excerpt"" character varying(600) NOT NULL,
                    ""Content"" text NOT NULL,
                    ""CoverImageUrl"" character varying(1000) NULL,
                    ""Category"" character varying(100) NOT NULL,
                    ""Tags"" text[] NOT NULL DEFAULT '{}',
                    ""AuthorName"" character varying(200) NOT NULL,
                    ""AuthorId"" uuid NULL,
                    ""IsPublished"" boolean NOT NULL DEFAULT false,
                    ""PublishedAt"" timestamp with time zone NULL,
                    ""CreatedAt"" timestamp with time zone NOT NULL,
                    ""UpdatedAt"" timestamp with time zone NULL,
                    ""ViewCount"" integer NOT NULL DEFAULT 0
                );
                CREATE UNIQUE INDEX IF NOT EXISTS ""IX_blog_posts_Slug"" ON blog_posts (""Slug"");
                CREATE INDEX IF NOT EXISTS ""IX_blog_posts_IsPublished"" ON blog_posts (""IsPublished"");
                CREATE INDEX IF NOT EXISTS ""IX_blog_posts_Category"" ON blog_posts (""Category"");
                CREATE INDEX IF NOT EXISTS ""IX_blog_posts_CreatedAt"" ON blog_posts (""CreatedAt"");
            ");
            app.Logger.LogInformation("Idempotent schema safety patch executed successfully.");
        }
        catch (Exception patchEx)
        {
            app.Logger.LogWarning(patchEx, "Schema safety patch warning (non-fatal)");
        }

        // Dedicated fail-safe for AspNetUsers profile picture columns before any UserManager query
        try
        {
            context.Database.ExecuteSqlRaw(@"
                ALTER TABLE IF EXISTS ""AspNetUsers"" ADD COLUMN IF NOT EXISTS profile_picture_url character varying(1000);
                ALTER TABLE IF EXISTS ""AspNetUsers"" ADD COLUMN IF NOT EXISTS profile_picture_public_id character varying(256);
            ");
            app.Logger.LogInformation("AspNetUsers profile picture safety columns verified.");
        }
        catch (Exception userColEx)
        {
            app.Logger.LogWarning(userColEx, "AspNetUsers profile picture columns verification notice");
        }

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

        // Seed initial admin account if missing
        var seedUsers = new[]
        {
            new { Email = "admin@trailblazer.edu", Name = "System Administrator", Role = "Admin", Password = "AdminPassword123!" }
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

        // Startup Data Reconciliation & Account Purges
        try
        {
            // 1. Purge specified user accounts and bot accounts (as instructed by system administrator)
            string[] accountsToPurge =
            [
                "oluwafikayomi.digital@gmail.com",
                "fagbenrocoa@gmail.com",
                "rup.e.lox.a31.4@gmail.com",
                "er.us.eb.an.8.5@gmail.com",
                "etoc.e.y.om.1.9.8@gmail.com"
            ];

            foreach (var emailToPurge in accountsToPurge)
            {
                var targetUser = userManager.FindByEmailAsync(emailToPurge).GetAwaiter().GetResult();
                if (targetUser != null)
                {
                    // Remove associated refresh tokens first
                    var userTokens = context.RefreshTokens.Where(rt => rt.UserId == targetUser.Id).ToList();
                    if (userTokens.Count != 0)
                    {
                        context.RefreshTokens.RemoveRange(userTokens);
                        context.SaveChanges();
                    }

                    var delRes = userManager.DeleteAsync(targetUser).GetAwaiter().GetResult();
                    if (delRes.Succeeded)
                    {
                        app.Logger.LogInformation("Purged requested account {Email}", emailToPurge);
                    }
                    else
                    {
                        app.Logger.LogWarning("Failed to purge account {Email}: {Errors}", emailToPurge, string.Join(", ", delRes.Errors.Select(e => e.Description)));
                    }
                }

                // Also clean up any associated staff_invitations
                var relatedInvites = context.StaffInvitations.Where(x => x.Email == emailToPurge).ToList();
                if (relatedInvites.Count != 0)
                {
                    context.StaffInvitations.RemoveRange(relatedInvites);
                    context.SaveChanges();
                    app.Logger.LogInformation("Purged staff_invitations for {Email}", emailToPurge);
                }
            }

            // 2. Ensure any remaining accepted student accounts strictly have only the Student role
            var studentInvites = context.StaffInvitations
                .Where(x => x.IsAccepted && x.Role == "Student")
                .Select(x => x.Email)
                .Distinct()
                .ToList();

            foreach (var sEmail in studentInvites)
            {
                var sUser = userManager.FindByEmailAsync(sEmail).GetAwaiter().GetResult();
                if (sUser != null)
                {
                    var uRoles = userManager.GetRolesAsync(sUser).GetAwaiter().GetResult();
                    if (uRoles.Contains("Instructor"))
                    {
                        userManager.RemoveFromRoleAsync(sUser, "Instructor").GetAwaiter().GetResult();
                        app.Logger.LogInformation("Reconciled {Email}: removed Instructor role", sEmail);
                    }
                    if (uRoles.Contains("Admin"))
                    {
                        userManager.RemoveFromRoleAsync(sUser, "Admin").GetAwaiter().GetResult();
                        app.Logger.LogInformation("Reconciled {Email}: removed Admin role", sEmail);
                    }
                    if (!uRoles.Contains("Student"))
                    {
                        userManager.AddToRoleAsync(sUser, "Student").GetAwaiter().GetResult();
                        app.Logger.LogInformation("Reconciled {Email}: assigned Student role", sEmail);
                    }
                }
            }

            // 3. Purge bot submissions from submissions table if any exist
            context.Database.ExecuteSqlRaw(@"
                DELETE FROM submissions 
                WHERE email IN ('rup.e.lox.a31.4@gmail.com', 'er.us.eb.an.8.5@gmail.com', 'etoc.e.y.om.1.9.8@gmail.com', 'oluwafikayomi.digital@gmail.com');
            ");
            app.Logger.LogInformation("Startup data reconciliation completed successfully.");

            // 4. Seed realistic academic and preparatory blog articles if the blog catalog is empty
            try
            {
                if (!context.BlogPosts.Any())
                {
                    var sampleBlogs = new List<BlogPost>
                    {
                        new()
                        {
                            Id = Guid.NewGuid(),
                            Title = "The Proven 90-Day Blueprint to Score 300+ in JAMB UTME",
                            Slug = "the-proven-90-day-blueprint-to-score-300-plus-in-jamb-utme",
                            Excerpt = "A comprehensive, step-by-step preparation framework designed by top tutors to systematically master the JAMB syllabus, eliminate CBT exam anxiety, and achieve exceptional scores.",
                            Category = "JAMB & UTME Tips",
                            Tags = ["JAMB", "UTME", "Exam Prep", "Study Strategy", "CBT"],
                            CoverImageUrl = "https://images.unsplash.com/photo-1434030216411-0b793f4b4173?auto=format&fit=crop&w=1200&q=80",
                            AuthorName = "Trailblazers Academic Directorate",
                            IsPublished = true,
                            PublishedAt = DateTimeOffset.UtcNow.AddDays(-14),
                            CreatedAt = DateTimeOffset.UtcNow.AddDays(-14),
                            ViewCount = 142,
                            Content = """
Scoring 300 and above in the Unified Tertiary Matriculation Examination (UTME) is not reserved for geniuses—it is the result of systematic preparation, syllabus discipline, and computer-based test (CBT) speed management. At Trailblazers Academy, our top performers consistently cross the 300 mark by executing this structured 90-day timeline.

---

### Month 1: Syllabus Mastery & Conceptual Clarity (Days 1–30)
The biggest mistake UTME candidates make is jumping straight into past questions without understanding foundational principles. JAMB questions test application, not just rote memorization.

- **Deconstruct the JAMB Syllabus**: Download the official JAMB brochure and syllabus for your four registered subjects. Never read aimlessly; every topic tested by JAMB is explicitly listed.
- **The High-Yield Subject Strategy**:
  - **Use of English (Compulsory)**: Dedicate 30 minutes daily to vocabulary in context, sentence structure, idioms, and oral forms (rhymes, vowel/consonant sounds, primary stress). Thoroughly study the assigned novel.
  - **Mathematics & Physics**: Focus heavily on mechanics, waves, optics, electricity, and calculus. Master dimensional analysis and unit conversions early.
  - **Chemistry & Biology**: Focus on organic chemistry reactions, stoichiometry, periodicity, genetics, ecology, and cell physiology.
- **Produce Concise Summary Sheets**: After finishing each topic, write a 1-page summary sheet containing key definitions, chemical equations, and mathematical formulae.

---

### Month 2: The 10-Year Past Question Method (Days 31–60)
JAMB rarely invents completely new question archetypes; they modify variables, inverse problem statements, or test complementary angles of the same curriculum concepts.

1. **Topical Past Question Drills**: Group past questions by topic rather than year. For instance, solve 50 consecutive questions solely on *Electrolysis* or *Indices* to identify examiner patterns.
2. **The "Error Journal" Technique**: When you get a question wrong, never just look at the correct letter (A, B, C, D). Record the full question in your personal Error Journal, explain *why* your initial logic failed, and re-solve it correctly without looking at answers.
3. **Master Calculator Independence**: JAMB's on-screen calculator only performs elementary arithmetic (+, -, *, /). Train your mind to do mental approximations, scientific notations (10^-3), and fractions rapidly.

---

### Month 3: Simulated CBT Environment & Speed Optimization (Days 61–90)
During the final month, theoretical knowledge must transform into lightning-fast execution on the computer screen.

- **The 40-Second Rule**: In UTME, you have 180 questions to answer across 120 minutes (2 hours). That allows roughly **40 seconds per question**, leaving 20 minutes for overall review.
- **Order of Execution**: Always start with your strongest subject to build psychological momentum and secure easy points before tackling calculation-intensive subjects.
- **Weekly Full-Length Mock Exams**: Take at least two timed mock exams weekly on the Trailblazers CBT portal under strict examination hall conditions—no phones, no pauses, and no notes.

> **Key Takeaway**: Success in JAMB is 60% subject mastery and 40% psychological composure. Consistency over 90 days will reliably outperform last-minute cramming every single time.
"""
                        },
                        new()
                        {
                            Id = Guid.NewGuid(),
                            Title = "WAEC & NECO Strategy: How to Secure Distinction (A1) Across Core Subjects",
                            Slug = "waec-and-neco-strategy-how-to-secure-distinction-a1-across-core-subjects",
                            Excerpt = "Discover the exact marking rubrics utilized by WAEC and NECO examiners, with actionable advice on essay structure, science practical procedures, and mathematics workings.",
                            Category = "WAEC & NECO Guides",
                            Tags = ["WAEC", "NECO", "SSCE", "O Level", "Academic Excellence"],
                            CoverImageUrl = "https://images.unsplash.com/photo-1524995997946-a1c2e315a42f?auto=format&fit=crop&w=1200&q=80",
                            AuthorName = "Dr. E. O. Adebayo (Senior Science Lead)",
                            IsPublished = true,
                            PublishedAt = DateTimeOffset.UtcNow.AddDays(-9),
                            CreatedAt = DateTimeOffset.UtcNow.AddDays(-9),
                            ViewCount = 108,
                            Content = """
Securing straight A1s in the West African Senior School Certificate Examination (WASSCE) or National Examinations Council (NECO) requires a completely different approach from objective-only CBT tests like JAMB. O'Level grading heavily penalizes poor presentation, missing mathematical steps, and grammatical carelessness in theory sections.

---

### Understanding the Examiner's Marking Scheme
Examiners do not award marks for what you *intended* to write; they award marks strictly based on explicit rubric criteria:
- **M-Marks (Method)**: Awarded for applying the correct formula or logical theorem, even if the final arithmetic calculation contains a minor error.
- **A-Marks (Accuracy)**: Awarded for obtaining correct intermediate and final numbers, contingent upon valid preceding method marks.
- **B-Marks (Independent)**: Awarded for standalone definitions, correct units, diagrams, or scientific labels.

---

### 1. Mathematics Theory (Paper 2): Show Every Step
Never write the final numerical answer without showing your mathematical working. If a quadratic equation question is worth 8 marks:
- Stating the formula: 1 Mark
- Correct substitution: 2 Marks
- Factorization / Simplification steps: 3 Marks
- Final solution with both roots: 2 Marks

*Writing just the final answer without working will get you at most 1 mark out of 8.* Always write clearly, underline final results, and double-check algebraic signs (+ vs -).

---

### 2. English Language (Paper 1): The Essay & Comprehension Formula
English Language is the prerequisite for admission across all tertiary institutions in Nigeria and abroad.
- **Continuous Writing (50 Marks)**: Choose the essay type that aligns with your strength. If writing an **Argumentative Essay**, maintain a clear stance throughout, use formal transitional phrases (*Furthermore, In light of this evidence, Conversely*), and eliminate informal vernacular.
- **Comprehension & Summary (30 Marks + 20 Marks)**:
  - In Comprehension: Avoid lifting whole sentences verbatim when asked to use your own words.
  - In Summary: Always write in **complete sentences** (do not use fragments or bullet dashes unless specified). Answer questions with concise, declarative points.

---

### 3. Science Practicals (Physics, Chemistry, Biology)
Practical papers carry up to 40% of your total science grade:
- **Physics**: In graphs, ensure your scales are uniform and occupy at least two-thirds of the graph page. Choose large triangle coordinates for slope calculations.
- **Chemistry**: In volumetric analysis (titration), record all burette readings to **two decimal places** (e.g., 24.50 cm3, never 24.5 cm3). Concordant titre values must be within 0.20 cm3 of each other.
- **Biology**: All specimen drawings must be drawn with sharp pencils, continuous (non-woody) lines, and horizontal label lines ending directly on the identified structures without arrowheads. State magnification accurately (e.g., x1.5).

With systematic preparation and attention to examiner rubrics, an A1 in every registered subject is entirely attainable.
"""
                        },
                        new()
                        {
                            Id = Guid.NewGuid(),
                            Title = "The Cognitive Science of Revision: Active Recall, Spaced Repetition & The Leitner Box",
                            Slug = "the-cognitive-science-of-revision-active-recall-and-spaced-repetition",
                            Excerpt = "Moving beyond passive rereading: How secondary school students can harness proven cognitive neuroscience principles to retain complex formulas and definitions permanently.",
                            Category = "Study Strategies",
                            Tags = ["Study Skills", "Active Recall", "Memory", "Productivity", "Student Success"],
                            CoverImageUrl = "https://images.unsplash.com/photo-1516321318423-f06f85e504b3?auto=format&fit=crop&w=1200&q=80",
                            AuthorName = "Trailblazers Cognitive Learning Lab",
                            IsPublished = true,
                            PublishedAt = DateTimeOffset.UtcNow.AddDays(-5),
                            CreatedAt = DateTimeOffset.UtcNow.AddDays(-5),
                            ViewCount = 89,
                            Content = """
Have you ever spent five consecutive hours reading a textbook, highlighting sentences in bright yellow, only to discover three days later that you remember almost nothing during a surprise test?

This is called the **Illusion of Competence**. Passive reading and highlighting feel effortless because your eyes recognize the words on the page, but recognition is neurological light years away from active retrieval under exam conditions.

---

### The Ebbinghaus Forgetting Curve
In 1885, German psychologist Hermann Ebbinghaus discovered that without active reinforcement, human beings forget **over 50% of newly learned material within 24 hours**, and **up to 70% within 48 hours**. 

The only proven cognitive antidote to this exponential memory decay is **Spaced Retrieval Practice**.

```
Day 1: Initial Learning -> 100% Retention
Day 2: Review (Recall after 24 hrs) -> Resets decay curve
Day 4: Second Recall -> Flattens decay further
Day 8: Third Recall -> Long-term consolidation
```

---

### Practical Tool 1: The Leitner 5-Box Flashcard Method
Create physical flashcards or digital flashcards for definitions, formulae, and historical timelines:
- **Front of Card**: The question or prompt (e.g., *"What is Lenz's Law of Electromagnetic Induction?"*)
- **Back of Card**: The concise, accurate scientific answer.

**How the 5-Box System Works**:
1. **Box 1 (Daily)**: All new cards start here. You test yourself on Box 1 every day.
2. If you answer a card correctly, it moves forward to **Box 2 (Every 3 Days)**.
3. If you get it right in Box 2, it moves to **Box 3 (Every Week)**.
4. If you get it right in Box 3, it moves to **Box 4 (Every 2 Weeks)**, then **Box 5 (Monthly)**.
5. **The Golden Rule**: *If you EVER get a card wrong at any stage, it instantly drops all the way back to Box 1!*

This forces your brain to spend 80% of its study time on the concepts you struggle with, and only 20% on what you have already mastered.

---

### Practical Tool 2: The Feynman Technique for Complex Concepts
Named after Nobel laureate physicist Richard Feynman, this 4-step framework tests whether you truly understand a concept:
1. **Choose a Topic**: (e.g., *Cellular Respiration* or *Ohm's Law*).
2. **Teach It to a 10-Year-Old**: Explain the concept out loud on a blank sheet of paper using simple everyday analogies, without using technical jargon.
3. **Identify Your Explanatory Gaps**: The exact moment you stumble, hesitate, or resort to textbook buzzwords, you have pinpointed a hole in your understanding.
4. **Return to the Source Material**: Re-read your notes specifically to patch that single gap, then explain it simply again.

Stop rereading passively. Test yourself actively every week on the Trailblazers CBT portal and watch your retention skyrocket.
"""
                        },
                        new()
                        {
                            Id = Guid.NewGuid(),
                            Title = "Navigating JAMB CAPS & Nigerian University Admissions: Merit, Catchment & Post-UTME",
                            Slug = "navigating-jamb-caps-and-nigerian-university-admissions-merit-catchment-post-utme",
                            Excerpt = "An authoritative guide for students and parents on calculating aggregate admission scores, understanding university quotas, and successfully navigating JAMB CAPS.",
                            Category = "University Admissions & Post-UTME",
                            Tags = ["University Admission", "JAMB CAPS", "Post-UTME", "Direct Entry", "Education Nigeria"],
                            CoverImageUrl = "https://images.unsplash.com/photo-1523240795612-9a054b0db644?auto=format&fit=crop&w=1200&q=80",
                            AuthorName = "Admissions & Career Advisory Unit",
                            IsPublished = true,
                            PublishedAt = DateTimeOffset.UtcNow.AddDays(-2),
                            CreatedAt = DateTimeOffset.UtcNow.AddDays(-2),
                            ViewCount = 164,
                            Content = """
Every year, thousands of qualified Nigerian students with UTME scores above 270 miss out on tertiary university admissions simply because they do not understand how the Central Admissions Processing System (CAPS) and institutional quota formulas operate.

Understanding the admission mechanics beforehand gives you a decisive strategic advantage.

---

### The Federal 3-Tier Admission Quota
All Federal Universities in Nigeria adhere to the following statutory quota distribution mandated by the Federal Ministry of Education:

1. **National Merit (45%)**: The top 45% of available departmental slots are awarded strictly on academic merit, regardless of the candidate's state of origin, religion, or background. If the merit cutoff for Medicine at UNILAG is 84.5%, anyone scoring 84.5% or above is entitled to admission.
2. **Catchment Areas (35%)**: 35% of admissions are allocated to applicants originating from the institution's designated geographical catchment states (e.g., South-West states for OAU and UNILAG, North-Central states for UNILORIN). Cutoff marks for catchment are generally 2 to 5 points lower than merit.
3. **Educationally Less Developed States - ELDS (20%)**: Reserved for candidates from designated states (such as Adamawa, Bauchi, Bayelsa, Ebonyi, Gombe, Jigawa, Kano, Kastina, Kebbi, Kogi, Kwara, Nasarawa, Niger, Plateau, Sokoto, Taraba, Yobe, and Zamfara) to promote educational equity across the federation.

---

### Understanding the Aggregate Score Formula
Most premier federal and state universities (including University of Lagos, University of Ibadan, and Obafemi Awolowo University) use a composite **100-Point Aggregate Formula**:

Aggregate Score = (JAMB Score / 8) + Post-UTME Score (or O'Level Points)

- **JAMB Contribution (50%)**: Your JAMB score out of 400 is divided by 8 to yield a score out of 50. (A score of 320 in JAMB gives you 320 / 8 = 40.0 points).
- **Post-UTME / O'Level Screening (50%)**: Derived from your performance in the computer-based Post-UTME screening exam or a calculated point system based on your five relevant O'Level grades (where A1 = 4 points, B2 = 3.6 points, etc.).

---

### Managing JAMB CAPS: Critical Rules
1. **Regularly Check "Transfer Approval"**: If your first-choice course is oversubscribed, an institution may recommend you for an alternative related course (e.g., from *Medicine & Surgery* to *Physiology* or *Biochemistry*). This will appear under the **Transfer Approval** tab on JAMB CAPS. If you do not accept or decline within the specified window, the offer expires.
2. **Accepting vs. Declining Admission**: Once an admission offer appears in green text on CAPS, accepting it is irreversible. Never accept an offer if you are actively awaiting a prioritized merit list from another institution where you meet all cutoff criteria.
3. **Upload Your O'Level Results Early**: Ensure your verified WAEC or NECO result is successfully uploaded to the JAMB central portal through an accredited CBT centre. An admission recommendation cannot be processed on CAPS if your O'Level profile displays "Awaiting Result".

For personalized career and admission counseling, consult the Trailblazers Academy Academic Guidance Unit during our weekday consultation hours.
"""
                        }
                    };

                    context.BlogPosts.AddRange(sampleBlogs);
                    context.SaveChanges();
                    app.Logger.LogInformation("Seeded 4 realistic academic blog articles successfully.");
                }
            }
            catch (Exception blogSeedEx)
            {
                app.Logger.LogWarning(blogSeedEx, "Blog seeding notice (non-fatal)");
            }
        }
        catch (Exception recEx)
        {
            app.Logger.LogWarning(recEx, "Startup data reconciliation notice (non-fatal)");
        }
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "An error occurred while initializing the database.");
    }
}

app.Run();
