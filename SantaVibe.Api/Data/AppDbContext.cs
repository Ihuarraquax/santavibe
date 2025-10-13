using Microsoft.EntityFrameworkCore;
using SantaVibe.Api.Models;

namespace SantaVibe.Api.Data;

/// <summary>
/// Application database context
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Group> Groups => Set<Group>();
    public DbSet<Participant> Participants => Set<Participant>();
    public DbSet<ExclusionRule> ExclusionRules => Set<ExclusionRule>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(u => u.Id);
            entity.HasIndex(u => u.Email).IsUnique();
            entity.Property(u => u.Email).HasMaxLength(255).IsRequired();
            entity.Property(u => u.FirstName).HasMaxLength(100).IsRequired();
            entity.Property(u => u.LastName).HasMaxLength(100).IsRequired();
            entity.Property(u => u.PasswordHash).IsRequired();
            entity.Property(u => u.CreatedAt).HasDefaultValueSql("NOW()");
        });

        // Group configuration
        modelBuilder.Entity<Group>(entity =>
        {
            entity.HasKey(g => g.Id);
            entity.HasIndex(g => g.InvitationCode).IsUnique();
            entity.Property(g => g.Name).HasMaxLength(200).IsRequired();
            entity.Property(g => g.InvitationCode).HasMaxLength(50);
            entity.Property(g => g.Budget).HasPrecision(10, 2);
            entity.Property(g => g.IsDrawPerformed).HasDefaultValue(false);
            entity.Property(g => g.CreatedAt).HasDefaultValueSql("NOW()");

            entity.HasOne(g => g.Organizer)
                .WithMany(u => u.OrganizedGroups)
                .HasForeignKey(g => g.OrganizerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Participant configuration
        modelBuilder.Entity<Participant>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.HasIndex(p => new { p.GroupId, p.UserId }).IsUnique();
            entity.Property(p => p.BudgetSuggestion).HasPrecision(10, 2);
            entity.Property(p => p.JoinedAt).HasDefaultValueSql("NOW()");

            entity.HasOne(p => p.Group)
                .WithMany(g => g.Participants)
                .HasForeignKey(p => p.GroupId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(p => p.User)
                .WithMany(u => u.Participations)
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(p => p.AssignedRecipient)
                .WithMany()
                .HasForeignKey(p => p.AssignedRecipientId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ExclusionRule configuration
        modelBuilder.Entity<ExclusionRule>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.GroupId, e.Participant1Id, e.Participant2Id }).IsUnique();

            entity.HasOne(e => e.Group)
                .WithMany(g => g.ExclusionRules)
                .HasForeignKey(e => e.GroupId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Participant1)
                .WithMany()
                .HasForeignKey(e => e.Participant1Id)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Participant2)
                .WithMany()
                .HasForeignKey(e => e.Participant2Id)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
