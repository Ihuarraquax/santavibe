using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SantaVibe.Api.Data.Entities;

namespace SantaVibe.Api.Data.Configurations;

/// <summary>
/// EF Core configuration for ApplicationUser entity
/// </summary>
public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        // Properties
        builder.Property(u => u.FirstName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(u => u.LastName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(u => u.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("NOW()");

        builder.Property(u => u.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);

        // Relationships
        builder.HasMany(u => u.OrganizedGroups)
            .WithOne(g => g.Organizer)
            .HasForeignKey(g => g.OrganizerUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(u => u.GroupParticipants)
            .WithOne(gp => gp.User)
            .HasForeignKey(gp => gp.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(u => u.AssignmentsAsSanta)
            .WithOne(a => a.Santa)
            .HasForeignKey(a => a.SantaUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(u => u.AssignmentsAsRecipient)
            .WithOne(a => a.Recipient)
            .HasForeignKey(a => a.RecipientUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(u => u.EmailNotifications)
            .WithOne(e => e.Recipient)
            .HasForeignKey(e => e.RecipientUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(u => u.ExclusionRulesAsUser1)
            .WithOne(er => er.User1)
            .HasForeignKey(er => er.UserId1)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(u => u.ExclusionRulesAsUser2)
            .WithOne(er => er.User2)
            .HasForeignKey(er => er.UserId2)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(u => u.ExclusionRulesCreated)
            .WithOne(er => er.CreatedBy)
            .HasForeignKey(er => er.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes (Identity indexes are auto-created)
        // Application-layer filtering for soft deletes: WHERE IsDeleted = FALSE
    }
}
