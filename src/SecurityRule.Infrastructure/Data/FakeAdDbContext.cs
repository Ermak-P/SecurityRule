using Microsoft.EntityFrameworkCore;
using SecurityRule.Infrastructure.Data.FakeAd;

namespace SecurityRule.Infrastructure.Data;

/// <summary>
/// EF Core database context for the FakeAd database.
/// This is a separate database from the main SecurityRule database and acts as
/// an in-process substitute for Active Directory during development and testing.
/// </summary>
public class FakeAdDbContext : DbContext
{
    public FakeAdDbContext(DbContextOptions<FakeAdDbContext> options) : base(options) { }

    public DbSet<AdUser> AdUsers => Set<AdUser>();
    public DbSet<AdGroup> AdGroups => Set<AdGroup>();
    public DbSet<AdUserGroupMembership> AdUserGroupMemberships => Set<AdUserGroupMembership>();
    public DbSet<AdGroupGroupMembership> AdGroupGroupMemberships => Set<AdGroupGroupMembership>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<AdUser>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.HasIndex(e => e.Name).IsUnique();
        });

        modelBuilder.Entity<AdGroup>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.HasIndex(e => e.Name).IsUnique();
        });

        modelBuilder.Entity<AdUserGroupMembership>(entity =>
        {
            entity.HasKey(e => new { e.UserId, e.GroupId });

            entity.HasOne(e => e.User)
                  .WithMany(u => u.GroupMemberships)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Group)
                  .WithMany(g => g.UserMemberships)
                  .HasForeignKey(e => e.GroupId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AdGroupGroupMembership>(entity =>
        {
            entity.HasKey(e => new { e.ParentGroupId, e.ChildGroupId });

            entity.HasOne(e => e.ParentGroup)
                  .WithMany(g => g.ChildMemberships)
                  .HasForeignKey(e => e.ParentGroupId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.ChildGroup)
                  .WithMany(g => g.ParentMemberships)
                  .HasForeignKey(e => e.ChildGroupId)
                  .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
