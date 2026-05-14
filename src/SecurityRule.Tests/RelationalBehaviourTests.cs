using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SecurityRule.Domain.Models;
using SecurityRule.Infrastructure.Data;
using SecurityRule.Infrastructure.Repositories;

namespace SecurityRule.Tests;

/// <summary>
/// Integration tests that run against SQLite in-memory to verify relational
/// constraints (unique indexes, FK integrity) that the EF InMemory provider silently ignores.
/// </summary>
[TestFixture]
public class RelationalBehaviourTests
{
    private SqliteConnection _connection = null!;
    private AppDbContext _context = null!;

    [SetUp]
    public void SetUp()
    {
        // Keep the connection open for the lifetime of the test so the in-memory DB persists.
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new AppDbContext(options);
        _context.Database.EnsureCreated();
    }

    [TearDown]
    public void TearDown()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    // ── Tag unique-index enforcement ──────────────────────────────────────────

    [Test]
    public async Task Tag_UniqueIndex_Enforced_ByDatabase()
    {
        // Tag.Name has a unique index in OnModelCreating.
        // EF InMemory ignores this; SQLite enforces it.
        _context.Tags.Add(new Tag { Name = "production" });
        await _context.SaveChangesAsync();

        // A second tag with the same name must violate the unique constraint.
        _context.Tags.Add(new Tag { Name = "production" });
        var act = async () => await _context.SaveChangesAsync();

        await act.Should().ThrowAsync<DbUpdateException>();
    }

    [Test]
    public async Task Tag_DifferentNames_AreInserted_Successfully()
    {
        _context.Tags.AddRange(new Tag { Name = "alpha" }, new Tag { Name = "beta" });
        await _context.SaveChangesAsync();

        var tags = await _context.Tags.ToListAsync();
        tags.Should().HaveCount(2);
    }

    // ── Server ↔ Tag many-to-many ─────────────────────────────────────────────

    [Test]
    public async Task Server_Tags_ManyToMany_StoredAndLoaded_CorrectlyViaSQLite()
    {
        var serverRepo = new ServerRepository(_context);

        var tag1 = new Tag { Name = "web" };
        var tag2 = new Tag { Name = "db" };
        _context.Tags.AddRange(tag1, tag2);
        await _context.SaveChangesAsync();

        var server = new Server
        {
            Name = "Tagged-Server",
            IpAddress = "10.0.0.1",
            OperatingSystem = "Linux",
            Tags = [tag1, tag2]
        };
        await serverRepo.AddAsync(server);

        var loaded = await serverRepo.GetByIdAsync(server.Id);
        loaded.Should().NotBeNull();
        loaded!.Tags.Should().HaveCount(2);
        loaded.Tags.Select(t => t.Name).Should().BeEquivalentTo(["web", "db"]);
    }

    // ── ServiceConnection FK integrity ────────────────────────────────────────

    [Test]
    public async Task ServiceConnection_RequiresExistingDestinationService()
    {
        // Non-existent DestinationServiceId = 9999 should fail on FK constraint
        // in a real relational DB. EF InMemory allows it silently.
        var conn = new ServiceConnection
        {
            DestinationServiceId = 9999,
            Protocol = "TCP"
        };

        _context.ServiceConnections.Add(conn);

        // SQLite enforces FK constraints after PRAGMA foreign_keys = ON (EF Core sets this).
        var act = async () => await _context.SaveChangesAsync();

        await act.Should().ThrowAsync<DbUpdateException>("because FK 9999 does not exist");
    }
}
