using FluentAssertions;
using Microsoft.EntityFrameworkCore;
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
        // Arrange
        var server = new Server { Name = "Server1", IpAddress = "192.168.1.1", OperatingSystem = "Linux" };

        // Act
        await _repository.AddAsync(server);

        // Assert
        var result = await _context.Servers.ToListAsync();
        result.Should().HaveCount(1);
        result[0].Name.Should().Be("Server1");
    }

    [Test]
    public async Task GetAllAsync_ShouldReturnAllServers()
    {
        // Arrange
        _context.Servers.AddRange(
            new Server { Name = "Server1", IpAddress = "192.168.1.1", OperatingSystem = "Linux" },
            new Server { Name = "Server2", IpAddress = "192.168.1.2", OperatingSystem = "Windows" }
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        result.Should().HaveCount(2);
    }

    [Test]
    public async Task GetByIdAsync_ShouldReturnCorrectServer()
    {
        // Arrange
        var server = new Server { Name = "Server1", IpAddress = "192.168.1.1", OperatingSystem = "Linux" };
        _context.Servers.Add(server);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdAsync(server.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Server1");
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
    public async Task UpdateAsync_ShouldUpdateServer()
    {
        // Arrange
        var server = new Server { Name = "Server1", IpAddress = "192.168.1.1", OperatingSystem = "Linux" };
        _context.Servers.Add(server);
        await _context.SaveChangesAsync();

        // Act
        server.Name = "UpdatedServer";
        await _repository.UpdateAsync(server);

        // Assert
        var result = await _context.Servers.FindAsync(server.Id);
        result!.Name.Should().Be("UpdatedServer");
    }

    [Test]
    public async Task DeleteAsync_ShouldRemoveServer()
    {
        // Arrange
        var server = new Server { Name = "Server1", IpAddress = "192.168.1.1", OperatingSystem = "Linux" };
        _context.Servers.Add(server);
        await _context.SaveChangesAsync();

        // Act
        await _repository.DeleteAsync(server.Id);

        // Assert
        var result = await _context.Servers.ToListAsync();
        result.Should().BeEmpty();
    }

    [Test]
    public async Task DeleteAsync_ShouldNotThrow_WhenNotFound()
    {
        // Arrange – empty database

        // Act
        var act = async () => await _repository.DeleteAsync(999);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Test]
    public async Task UpdateAsync_ShouldAssociateServices()
    {
        // Arrange
        var service1 = new AppService { Name = "Svc1", UserName = "domain\\svc1" };
        var service2 = new AppService { Name = "Svc2", UserName = "domain\\svc2" };
        _context.AppServices.AddRange(service1, service2);
        var server = new Server { Name = "Server1", IpAddress = "10.0.0.1", OperatingSystem = "Linux" };
        _context.Servers.Add(server);
        await _context.SaveChangesAsync();

        // Act — associate both services
        server.Services = [service1, service2];
        await _repository.UpdateAsync(server);

        // Assert
        var updated = await _repository.GetByIdAsync(server.Id);
        updated!.Services.Should().HaveCount(2);
        updated.Services.Select(s => s.Name).Should().BeEquivalentTo(["Svc1", "Svc2"]);
    }

    [Test]
    public async Task UpdateAsync_ShouldReplaceServices()
    {
        // Arrange
        var service1 = new AppService { Name = "Svc1", UserName = "domain\\svc1" };
        var service2 = new AppService { Name = "Svc2", UserName = "domain\\svc2" };
        _context.AppServices.AddRange(service1, service2);
        var server = new Server
        {
            Name = "Server1",
            IpAddress = "10.0.0.1",
            OperatingSystem = "Linux",
            Services = [service1]
        };
        _context.Servers.Add(server);
        await _context.SaveChangesAsync();

        // Act — replace service1 with service2
        server.Services = [service2];
        await _repository.UpdateAsync(server);

        // Assert
        var updated = await _repository.GetByIdAsync(server.Id);
        updated!.Services.Should().HaveCount(1);
        updated.Services.Single().Name.Should().Be("Svc2");
    }
}
