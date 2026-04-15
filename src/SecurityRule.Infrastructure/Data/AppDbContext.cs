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
            entity.HasOne(e => e.Server)
                  .WithMany(s => s.Services)
                  .HasForeignKey(e => e.ServerId)
                  .OnDelete(DeleteBehavior.Cascade);
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
    }
}
