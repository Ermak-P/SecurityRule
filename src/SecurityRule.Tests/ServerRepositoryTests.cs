using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using SecurityRule.Domain.Models;
using SecurityRule.Infrastructure.Data;
using SecurityRule.Infrastructure.Repositories;

namespace SecurityRule.Tests;

[TestFixture]
public class ServerRepositoryTests
{
    private AppDbContext _context = null!;
    private ServerRepository _repository = null!;

    [SetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new AppDbContext(options);
        _repository = new ServerRepository(_context);
    }

    [TearDown]
    public void TearDown()
    {
        _context.Dispose();
    }

    [Test]
    public async Task AddAsync_ShouldAddServer()
    {
        var server = new Server { Name = "Server1", IpAddress = "192.168.1.1", OperatingSystem = "Linux" };

        await _repository.AddAsync(server);

        var result = await _context.Servers.ToListAsync();
        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].Name, Is.EqualTo("Server1"));
    }

    [Test]
    public async Task GetAllAsync_ShouldReturnAllServers()
    {
        _context.Servers.AddRange(
            new Server { Name = "Server1", IpAddress = "192.168.1.1", OperatingSystem = "Linux" },
            new Server { Name = "Server2", IpAddress = "192.168.1.2", OperatingSystem = "Windows" }
        );
        await _context.SaveChangesAsync();

        var result = await _repository.GetAllAsync();

        Assert.That(result.Count(), Is.EqualTo(2));
    }

    [Test]
    public async Task GetByIdAsync_ShouldReturnCorrectServer()
    {
        var server = new Server { Name = "Server1", IpAddress = "192.168.1.1", OperatingSystem = "Linux" };
        _context.Servers.Add(server);
        await _context.SaveChangesAsync();

        var result = await _repository.GetByIdAsync(server.Id);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Name, Is.EqualTo("Server1"));
    }

    [Test]
    public async Task GetByIdAsync_ShouldReturnNull_WhenNotFound()
    {
        var result = await _repository.GetByIdAsync(999);

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task UpdateAsync_ShouldUpdateServer()
    {
        var server = new Server { Name = "Server1", IpAddress = "192.168.1.1", OperatingSystem = "Linux" };
        _context.Servers.Add(server);
        await _context.SaveChangesAsync();

        server.Name = "UpdatedServer";
        await _repository.UpdateAsync(server);

        var result = await _context.Servers.FindAsync(server.Id);
        Assert.That(result!.Name, Is.EqualTo("UpdatedServer"));
    }

    [Test]
    public async Task DeleteAsync_ShouldRemoveServer()
    {
        var server = new Server { Name = "Server1", IpAddress = "192.168.1.1", OperatingSystem = "Linux" };
        _context.Servers.Add(server);
        await _context.SaveChangesAsync();

        await _repository.DeleteAsync(server.Id);

        var result = await _context.Servers.ToListAsync();
        Assert.That(result, Is.Empty);
    }

    [Test]
    public async Task DeleteAsync_ShouldNotThrow_WhenNotFound()
    {
        Assert.DoesNotThrowAsync(() => _repository.DeleteAsync(999));
    }
}
