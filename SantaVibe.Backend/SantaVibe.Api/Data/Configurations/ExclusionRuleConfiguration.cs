using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SantaVibe.Api.Data.Entities;

namespace SantaVibe.Api.Data.Configurations;

/// <summary>
/// EF Core configuration for ExclusionRule entity
/// </summary>
public class ExclusionRuleConfiguration : IEntityTypeConfiguration<ExclusionRule>
{
    public void Configure(EntityTypeBuilder<ExclusionRule> builder)
    {
        // Primary Key
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .ValueGeneratedNever()
            .IsRequired();
        // Properties
        builder.Property(er => er.UserId1)
            .IsRequired()
            .HasMaxLength(450);

        builder.Property(er => er.UserId2)
            .IsRequired()
            .HasMaxLength(450);

        builder.Property(er => er.CreatedByUserId)
            .IsRequired()
            .HasMaxLength(450);

        builder.Property(er => er.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("NOW()");

        // Table configuration with check constraints (EF Core 9+)
        builder.ToTable(t => t.HasCheckConstraint(
            "CK_ExclusionRules_DifferentUsers",
            "\"UserId1\" <> \"UserId2\""));

        // Unique Constraints
        builder.HasIndex(er => new { er.GroupId, er.UserId1, er.UserId2 })
            .IsUnique()
            .HasDatabaseName("IX_ExclusionRules_Unique");

        // Indexes
        builder.HasIndex(er => er.GroupId)
            .HasDatabaseName("IX_ExclusionRules_GroupId");

        // Relationships
        builder.HasOne(er => er.Group)
            .WithMany(g => g.ExclusionRules)
            .HasForeignKey(er => er.GroupId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(er => er.User1)
            .WithMany(u => u.ExclusionRulesAsUser1)
            .HasForeignKey(er => er.UserId1)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(er => er.User2)
            .WithMany(u => u.ExclusionRulesAsUser2)
            .HasForeignKey(er => er.UserId2)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(er => er.CreatedBy)
            .WithMany(u => u.ExclusionRulesCreated)
            .HasForeignKey(er => er.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
