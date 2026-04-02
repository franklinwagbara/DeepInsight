using DeepInsight.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace DeepInsight.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<Session> Sessions => Set<Session>();
    public DbSet<PageVisit> PageVisits => Set<PageVisit>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Email).HasMaxLength(256).IsRequired();
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.PasswordHash).IsRequired();
            entity.Property(e => e.Name).HasMaxLength(200);
        });

        modelBuilder.Entity<Project>(entity =>
        {
            entity.ToTable("projects");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Domain).HasMaxLength(500);
            entity.HasIndex(e => e.TrackingId).IsUnique();
            entity.HasOne(e => e.User)
                  .WithMany(u => u.Projects)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Session>(entity =>
        {
            entity.ToTable("sessions");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.SessionId).HasMaxLength(100).IsRequired();
            entity.HasIndex(e => new { e.ProjectId, e.SessionId }).IsUnique();
            entity.HasIndex(e => e.StartedAt);
            entity.HasIndex(e => e.ProjectId);
            entity.Property(e => e.DeviceType).HasMaxLength(50);
            entity.Property(e => e.Browser).HasMaxLength(100);
            entity.Property(e => e.Os).HasMaxLength(100);
            entity.Property(e => e.Country).HasMaxLength(100);
            entity.Property(e => e.City).HasMaxLength(200);
            entity.Property(e => e.IpAnonymized).HasMaxLength(50);
            entity.Property(e => e.UserId).HasMaxLength(200);
            entity.HasOne(e => e.Project)
                  .WithMany(p => p.Sessions)
                  .HasForeignKey(e => e.ProjectId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PageVisit>(entity =>
        {
            entity.ToTable("page_visits");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Url).HasMaxLength(2000).IsRequired();
            entity.Property(e => e.Title).HasMaxLength(500);
            entity.HasOne(e => e.Session)
                  .WithMany(s => s.Pages)
                  .HasForeignKey(e => e.SessionDbId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
