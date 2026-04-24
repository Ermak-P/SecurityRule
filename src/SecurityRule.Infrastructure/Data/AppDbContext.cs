using Microsoft.EntityFrameworkCore;
using SecurityRule.Domain.Models;

namespace SecurityRule.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
        Database.EnsureCreated();
    }

    public DbSet<Server> Servers => Set<Server>();
    public DbSet<AppService> AppServices => Set<AppService>();
    public DbSet<Certificate> Certificates => Set<Certificate>();
    public DbSet<ServiceConnection> ServiceConnections => Set<ServiceConnection>();
    public DbSet<OperatingSystemOption> OperatingSystemOptions => Set<OperatingSystemOption>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Group> Groups => Set<Group>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Server>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.IpAddress).IsRequired().HasMaxLength(45);
            entity.Property(e => e.OperatingSystem).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(1000);
        });

        modelBuilder.Entity<AppService>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.UserName).IsRequired().HasMaxLength(200);
            entity.HasMany(e => e.Servers)
                  .WithMany(s => s.Services)
                  .UsingEntity(j => j.ToTable("ServerServices"));
            entity.HasMany(e => e.Certificates)
                  .WithMany(c => c.Services)
                  .UsingEntity(j => j.ToTable("ServiceCertificates"));
            entity.HasOne(e => e.User)
                  .WithMany(u => u.Services)
                  .HasForeignKey(e => e.UserId)
                  .IsRequired(false)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Certificate>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.SerialNumber).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Thumbprint).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.RequestNumber).IsRequired().HasMaxLength(200);
        });

        modelBuilder.Entity<ServiceConnection>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Protocol).HasMaxLength(10);

            entity.HasOne(e => e.SourceServer)
                  .WithMany(s => s.SourceConnections)
                  .HasForeignKey(e => e.SourceServerId)
                  .IsRequired(false)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.SourceService)
                  .WithMany(s => s.SourceConnections)
                  .HasForeignKey(e => e.SourceServiceId)
                  .IsRequired(false)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.DestinationServer)
                  .WithMany(s => s.DestinationConnections)
                  .HasForeignKey(e => e.DestinationServerId)
                  .IsRequired(false)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.DestinationService)
                  .WithMany(s => s.DestinationConnections)
                  .HasForeignKey(e => e.DestinationServiceId)
                  .IsRequired()
                  .OnDelete(DeleteBehavior.Restrict);
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

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.HasOne(e => e.Certificate)
                  .WithMany(c => c.Users)
                  .HasForeignKey(e => e.CertificateId)
                  .IsRequired(false)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Group>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(1000);
        });
    }
}
