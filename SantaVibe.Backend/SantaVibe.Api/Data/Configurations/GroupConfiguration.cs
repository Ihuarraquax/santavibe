using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SantaVibe.Api.Data.Entities;

namespace SantaVibe.Api.Data.Configurations;

/// <summary>
/// EF Core configuration for Group entity
/// </summary>
public class GroupConfiguration : IEntityTypeConfiguration<Group>
{
    public void Configure(EntityTypeBuilder<Group> builder)
    {
        // Primary Key
        builder.HasKey(g => g.Id);

        builder.Property(g => g.Id)
            .HasDefaultValueSql("gen_random_uuid()");

        // Properties
        builder.Property(g => g.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(g => g.OrganizerUserId)
            .IsRequired()
            .HasMaxLength(450);

        builder.Property(g => g.Budget)
            .HasPrecision(10, 2);

        builder.Property(g => g.InvitationToken)
            .IsRequired()
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(g => g.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("NOW()");

        // Unique Constraints
        builder.HasIndex(g => g.InvitationToken)
            .IsUnique()
            .HasDatabaseName("IX_Groups_InvitationToken");

        // Indexes
        builder.HasIndex(g => g.OrganizerUserId)
            .HasDatabaseName("IX_Groups_OrganizerUserId");

        // Partial index for active groups (requires raw SQL in migration)
        // CREATE INDEX IX_Groups_DrawCompletedAt ON Groups(DrawCompletedAt) WHERE DrawCompletedAt IS NULL;

        // Relationships
        builder.HasOne(g => g.Organizer)
            .WithMany(u => u.OrganizedGroups)
            .HasForeignKey(g => g.OrganizerUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(g => g.GroupParticipants)
            .WithOne(gp => gp.Group)
            .HasForeignKey(gp => gp.GroupId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(g => g.ExclusionRules)
            .WithOne(er => er.Group)
            .HasForeignKey(er => er.GroupId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(g => g.Assignments)
            .WithOne(a => a.Group)
            .HasForeignKey(a => a.GroupId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(g => g.EmailNotifications)
            .WithOne(e => e.Group)
            .HasForeignKey(e => e.GroupId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
