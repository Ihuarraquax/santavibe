using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SantaVibe.Api.Data.Entities;

namespace SantaVibe.Api.Data.Configurations;

/// <summary>
/// EF Core configuration for EmailNotification entity
/// </summary>
public class EmailNotificationConfiguration : IEntityTypeConfiguration<EmailNotification>
{
    public void Configure(EntityTypeBuilder<EmailNotification> builder)
    {
        // Primary Key
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasDefaultValueSql("gen_random_uuid()");

        // Properties
        builder.Property(e => e.Type)
            .IsRequired()
            .HasMaxLength(50)
            .HasConversion<string>(); // Store enum as string

        builder.Property(e => e.RecipientUserId)
            .IsRequired()
            .HasMaxLength(450);

        builder.Property(e => e.ScheduledAt)
            .IsRequired();

        builder.Property(e => e.AttemptCount)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(e => e.LastError)
            .HasColumnType("text");

        // Indexes
        // Partial index for queue processing (requires raw SQL in migration)
        // CREATE INDEX IX_EmailNotifications_Queue ON EmailNotifications(ScheduledAt, SentAt) WHERE SentAt IS NULL;

        builder.HasIndex(e => e.RecipientUserId)
            .HasDatabaseName("IX_EmailNotifications_RecipientUserId");

        builder.HasIndex(e => e.GroupId)
            .HasDatabaseName("IX_EmailNotifications_GroupId");

        // Relationships
        builder.HasOne(e => e.Recipient)
            .WithMany(u => u.EmailNotifications)
            .HasForeignKey(e => e.RecipientUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Group)
            .WithMany(g => g.EmailNotifications)
            .HasForeignKey(e => e.GroupId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
