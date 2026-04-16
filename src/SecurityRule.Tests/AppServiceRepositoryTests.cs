using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using SecurityRule.Domain.Models;
using SecurityRule.Infrastructure.Data;
using SecurityRule.Infrastructure.Repositories;

namespace SecurityRule.Tests;

[TestFixture]
public class AppServiceRepositoryTests
{
    private AppDbContext _context = null!;
    private AppServiceRepository _repository = null!;

    [SetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new AppDbContext(options);
        _repository = new AppServiceRepository(_context);
    }

    [TearDown]
    public void TearDown()
    {
        _context.Dispose();
    }

    private async Task<Server> CreateServerAsync()
    {
        var server = new Server { Name = "TestServer", IpAddress = "10.0.0.1", OperatingSystem = "Linux" };
        _context.Servers.Add(server);
        await _context.SaveChangesAsync();
        return server;
    }

    [Test]
    public async Task AddAsync_ShouldAddService()
    {
        var server = await CreateServerAsync();
        var service = new AppService { Name = "MyService", ServerId = server.Id, AdAccountName = "domain\\svc" };

        await _repository.AddAsync(service);

        var result = await _context.AppServices.ToListAsync();
        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].Name, Is.EqualTo("MyService"));
    }

    [Test]
    public async Task GetAllAsync_ShouldReturnAllServices()
    {
        var server = await CreateServerAsync();
        _context.AppServices.AddRange(
            new AppService { Name = "Svc1", ServerId = server.Id, AdAccountName = "domain\\svc1" },
            new AppService { Name = "Svc2", ServerId = server.Id, AdAccountName = "domain\\svc2" }
        );
        await _context.SaveChangesAsync();

        var result = await _repository.GetAllAsync();

        Assert.That(result.Count(), Is.EqualTo(2));
    }

    [Test]
    public async Task GetByIdAsync_ShouldReturnCorrectService()
    {
        var server = await CreateServerAsync();
        var service = new AppService { Name = "Svc1", ServerId = server.Id, AdAccountName = "domain\\svc1" };
        _context.AppServices.Add(service);
        await _context.SaveChangesAsync();

        var result = await _repository.GetByIdAsync(service.Id);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Name, Is.EqualTo("Svc1"));
    }

    [Test]
    public async Task GetByIdAsync_ShouldReturnNull_WhenNotFound()
    {
        var result = await _repository.GetByIdAsync(999);

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task UpdateAsync_ShouldUpdateService()
    {
        var server = await CreateServerAsync();
        var service = new AppService { Name = "Svc1", ServerId = server.Id, AdAccountName = "domain\\svc1" };
        _context.AppServices.Add(service);
        await _context.SaveChangesAsync();

        service.Name = "UpdatedSvc";
        await _repository.UpdateAsync(service);

        var result = await _context.AppServices.FindAsync(service.Id);
        Assert.That(result!.Name, Is.EqualTo("UpdatedSvc"));
    }

    [Test]
    public async Task DeleteAsync_ShouldRemoveService()
    {
        var server = await CreateServerAsync();
        var service = new AppService { Name = "Svc1", ServerId = server.Id, AdAccountName = "domain\\svc1" };
        _context.AppServices.Add(service);
        await _context.SaveChangesAsync();

        await _repository.DeleteAsync(service.Id);

        var result = await _context.AppServices.ToListAsync();
        Assert.That(result, Is.Empty);
    }
}
