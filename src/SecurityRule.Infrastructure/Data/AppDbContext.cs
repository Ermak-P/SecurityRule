using Microsoft.EntityFrameworkCore;
using SecurityRule.Domain.Models;

namespace SecurityRule.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Server> Servers => Set<Server>();
    public DbSet<AppService> AppServices => Set<AppService>();
    public DbSet<Certificate> Certificates => Set<Certificate>();
    public DbSet<FirewallRule> FirewallRules => Set<FirewallRule>();
    public DbSet<OperatingSystemOption> OperatingSystemOptions => Set<OperatingSystemOption>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Server>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.IpAddress).IsRequired().HasMaxLength(45);
            entity.Property(e => e.OperatingSystem).IsRequired().HasMaxLength(100);
        });

        modelBuilder.Entity<AppService>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.AdAccountName).IsRequired().HasMaxLength(200);
            entity.HasMany(e => e.Servers)
                  .WithMany(s => s.Services)
                  .UsingEntity(j => j.ToTable("ServerServices"));
            entity.HasMany(e => e.Certificates)
                  .WithMany(c => c.Services)
                  .UsingEntity(j => j.ToTable("ServiceCertificates"));
        });

        modelBuilder.Entity<Certificate>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Description).HasMaxLength(1000);
        });

        modelBuilder.Entity<FirewallRule>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.SourceIp).IsRequired().HasMaxLength(45);
            entity.Property(e => e.DestinationIp).IsRequired().HasMaxLength(45);
            entity.Property(e => e.Description).HasMaxLength(1000);
        });

        modelBuilder.Entity<OperatingSystemOption>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.HasData(
                new OperatingSystemOption { Id = 1, Name = "Windows 11" },
                new OperatingSystemOption { Id = 2, Name = "Windows Server 2022" }
            );
        });
    }
}
