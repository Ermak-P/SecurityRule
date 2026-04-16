using FluentAssertions;
using Microsoft.EntityFrameworkCore;
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
        // Arrange
        var server = await CreateServerAsync();
        var service = new AppService { Name = "MyService", AdAccountName = "domain\\svc", Servers = [server] };

        // Act
        await _repository.AddAsync(service);

        // Assert
        var result = await _context.AppServices.ToListAsync();
        result.Should().HaveCount(1);
        result[0].Name.Should().Be("MyService");
    }

    [Test]
    public async Task GetAllAsync_ShouldReturnAllServices()
    {
        // Arrange
        var server = await CreateServerAsync();
        _context.AppServices.AddRange(
            new AppService { Name = "Svc1", AdAccountName = "domain\\svc1", Servers = [server] },
            new AppService { Name = "Svc2", AdAccountName = "domain\\svc2", Servers = [server] }
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        result.Should().HaveCount(2);
    }

    [Test]
    public async Task GetByIdAsync_ShouldReturnCorrectService()
    {
        // Arrange
        var server = await CreateServerAsync();
        var service = new AppService { Name = "Svc1", AdAccountName = "domain\\svc1", Servers = [server] };
        _context.AppServices.Add(service);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdAsync(service.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Svc1");
    }

    [Test]
    public async Task GetByIdAsync_ShouldReturnNull_WhenNotFound()
    {
        // Arrange – empty database

        // Act
        var result = await _repository.GetByIdAsync(999);

        // Assert
        result.Should().BeNull();
    }

    [Test]
    public async Task GetByIdAsync_ShouldIncludeServers()
    {
        // Arrange
        var server = await CreateServerAsync();
        var service = new AppService { Name = "Svc1", AdAccountName = "domain\\svc1", Servers = [server] };
        _context.AppServices.Add(service);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdAsync(service.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Servers.Should().HaveCount(1);
        result.Servers.First().Name.Should().Be("TestServer");
    }

    [Test]
    public async Task UpdateAsync_ShouldUpdateService()
    {
        // Arrange
        var server = await CreateServerAsync();
        var service = new AppService { Name = "Svc1", AdAccountName = "domain\\svc1", Servers = [server] };
        _context.AppServices.Add(service);
        await _context.SaveChangesAsync();

        // Act
        service.Name = "UpdatedSvc";
        await _repository.UpdateAsync(service);

        // Assert
        var result = await _context.AppServices.FindAsync(service.Id);
        result!.Name.Should().Be("UpdatedSvc");
    }

    [Test]
    public async Task DeleteAsync_ShouldRemoveService()
    {
        // Arrange
        var server = await CreateServerAsync();
        var service = new AppService { Name = "Svc1", AdAccountName = "domain\\svc1", Servers = [server] };
        _context.AppServices.Add(service);
        await _context.SaveChangesAsync();

        // Act
        await _repository.DeleteAsync(service.Id);

        // Assert
        var result = await _context.AppServices.ToListAsync();
        result.Should().BeEmpty();
    }

    [Test]
    public async Task AddAsync_ServiceCanBeOnMultipleServers()
    {
        // Arrange
        var server1 = new Server { Name = "Server1", IpAddress = "10.0.0.1", OperatingSystem = "Linux" };
        var server2 = new Server { Name = "Server2", IpAddress = "10.0.0.2", OperatingSystem = "Windows" };
        _context.Servers.AddRange(server1, server2);
        await _context.SaveChangesAsync();
        var service = new AppService { Name = "SharedSvc", AdAccountName = "domain\\shared", Servers = [server1, server2] };

        // Act
        await _repository.AddAsync(service);

        // Assert
        var result = await _repository.GetByIdAsync(service.Id);
        result.Should().NotBeNull();
        result!.Servers.Should().HaveCount(2);
    }
}
