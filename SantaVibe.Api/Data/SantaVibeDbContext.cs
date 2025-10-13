using Microsoft.EntityFrameworkCore;
using SantaVibe.Api.Models;

namespace SantaVibe.Api.Data;

public class SantaVibeDbContext : DbContext
{
    public SantaVibeDbContext(DbContextOptions<SantaVibeDbContext> options) : base(options)
    {
    }

    public DbSet<Group> Groups { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Group>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.Budget).HasColumnType("decimal(18,2)");
            entity.Property(e => e.OrganizerId).IsRequired();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.HasIndex(e => e.InvitationCode).IsUnique();
        });
    }
}
