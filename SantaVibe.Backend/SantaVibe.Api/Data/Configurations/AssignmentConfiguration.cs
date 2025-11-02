using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SantaVibe.Api.Data.Entities;

namespace SantaVibe.Api.Data.Configurations;

/// <summary>
/// EF Core configuration for Assignment entity
/// </summary>
public class AssignmentConfiguration : IEntityTypeConfiguration<Assignment>
{
    public void Configure(EntityTypeBuilder<Assignment> builder)
    {
        // Primary Key
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .ValueGeneratedNever()
            .IsRequired();

        // Properties
        builder.Property(a => a.SantaUserId)
            .IsRequired()
            .HasMaxLength(450);

        builder.Property(a => a.RecipientUserId)
            .IsRequired()
            .HasMaxLength(450);

        builder.Property(a => a.AssignedAt)
            .IsRequired()
            .HasDefaultValueSql("NOW()");

        // Table configuration with check constraints (EF Core 9+)
        builder.ToTable(t => t.HasCheckConstraint(
            "CK_Assignments_NoSelfAssignment",
            "\"SantaUserId\" <> \"RecipientUserId\""));

        // Unique Constraints - one Santa per group, one Recipient per group
        builder.HasIndex(a => new { a.GroupId, a.SantaUserId })
            .IsUnique()
            .HasDatabaseName("IX_Assignments_GroupId_SantaUserId");

        builder.HasIndex(a => new { a.GroupId, a.RecipientUserId })
            .IsUnique()
            .HasDatabaseName("IX_Assignments_GroupId_RecipientUserId");

        // Indexes
        builder.HasIndex(a => a.SantaUserId)
            .HasDatabaseName("IX_Assignments_SantaUserId");

        // Relationships
        builder.HasOne(a => a.Group)
            .WithMany(g => g.Assignments)
            .HasForeignKey(a => a.GroupId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(a => a.Santa)
            .WithMany(u => u.AssignmentsAsSanta)
            .HasForeignKey(a => a.SantaUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.Recipient)
            .WithMany(u => u.AssignmentsAsRecipient)
            .HasForeignKey(a => a.RecipientUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
