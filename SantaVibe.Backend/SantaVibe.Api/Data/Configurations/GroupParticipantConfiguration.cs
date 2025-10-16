using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SantaVibe.Api.Data.Entities;

namespace SantaVibe.Api.Data.Configurations;

/// <summary>
/// EF Core configuration for GroupParticipant entity
/// </summary>
public class GroupParticipantConfiguration : IEntityTypeConfiguration<GroupParticipant>
{
    public void Configure(EntityTypeBuilder<GroupParticipant> builder)
    {
        // Composite Primary Key
        builder.HasKey(gp => new { gp.GroupId, gp.UserId });

        // Properties
        builder.Property(gp => gp.UserId)
            .IsRequired()
            .HasMaxLength(450);

        builder.Property(gp => gp.BudgetSuggestion)
            .HasPrecision(10, 2);

        builder.Property(gp => gp.JoinedAt)
            .IsRequired()
            .HasDefaultValueSql("NOW()");

        builder.Property(gp => gp.WishlistContent)
            .HasColumnType("text");

        // Indexes
        // Composite PK automatically creates index on (GroupId, UserId)

        // Reverse lookup index: user's groups
        builder.HasIndex(gp => gp.UserId)
            .HasDatabaseName("IX_GroupParticipants_UserId");

        // Relationships
        builder.HasOne(gp => gp.Group)
            .WithMany(g => g.GroupParticipants)
            .HasForeignKey(gp => gp.GroupId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(gp => gp.User)
            .WithMany(u => u.GroupParticipants)
            .HasForeignKey(gp => gp.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
