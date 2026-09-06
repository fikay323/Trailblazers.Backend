using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Trailblazers.Backend.Core.Domain.Entities;

namespace Trailblazers.Backend.Infrastructure.Persistence.Configurations
{
    public class StaffInvitationConfiguration : IEntityTypeConfiguration<StaffInvitation>
    {
        public void Configure(EntityTypeBuilder<StaffInvitation> builder)
        {
            builder.ToTable("staff_invitations");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Email)
                .HasMaxLength(256)
                .IsRequired();

            builder.Property(x => x.FullName)
                .HasMaxLength(200)
                .IsRequired();

            builder.Property(x => x.Role)
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(x => x.TokenHash)
                .HasMaxLength(128)
                .IsRequired();

            builder.Property(x => x.InvitedByUserName)
                .HasMaxLength(200);

            builder.HasIndex(x => x.Email);
            builder.HasIndex(x => x.TokenHash);
            builder.HasIndex(x => x.IsAccepted);
        }
    }
}
