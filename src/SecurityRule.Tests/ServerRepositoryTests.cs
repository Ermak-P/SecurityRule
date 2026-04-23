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

    [Test]
    public async Task GetByIdAsync_ShouldIncludeSourceFirewallRules()
    {
        // Arrange
        var server = new Server { Name = "FW-Server", IpAddress = "10.0.9.1", OperatingSystem = "Linux" };
        var dstSrv = new Server { Name = "Dst-Server", IpAddress = "10.0.9.2", OperatingSystem = "Linux" };
        var srcSvc = new AppService { Name = "Src-Service", UserName = "domain\\src" };
        var dstSvc = new AppService { Name = "Dst-Service", UserName = "domain\\dst" };
        _context.Servers.AddRange(server, dstSrv);
        _context.AppServices.AddRange(srcSvc, dstSvc);
        await _context.SaveChangesAsync();

        var rule = new FirewallRule
        {
            SourceServerId       = server.Id,
            SourceServiceId      = srcSvc.Id,
            DestinationServerId  = dstSrv.Id,
            DestinationServiceId = dstSvc.Id,
            Protocol = "TCP", Action = "Allow", Direction = "Inbound",
            Description = "ServerFWRule"
        };
        _context.FirewallRules.Add(rule);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdAsync(server.Id);

        // Assert
        result.Should().NotBeNull();
        result!.SourceFirewallRules.Should().HaveCount(1);
        result.SourceFirewallRules.First().Description.Should().Be("ServerFWRule");
    }

    [Test]
    public async Task GetByIdAsync_ShouldIncludeDestinationFirewallRules()
    {
        // Arrange
        var srcSrv = new Server { Name = "Src-Server", IpAddress = "10.0.8.1", OperatingSystem = "Linux" };
        var server = new Server { Name = "Dst-FW-Server", IpAddress = "10.0.8.2", OperatingSystem = "Linux" };
        var srcSvc = new AppService { Name = "Src-Service", UserName = "domain\\src" };
        var dstSvc = new AppService { Name = "Dst-Service", UserName = "domain\\dst" };
        _context.Servers.AddRange(srcSrv, server);
        _context.AppServices.AddRange(srcSvc, dstSvc);
        await _context.SaveChangesAsync();

        var rule = new FirewallRule
        {
            SourceServerId       = srcSrv.Id,
            SourceServiceId      = srcSvc.Id,
            DestinationServerId  = server.Id,
            DestinationServiceId = dstSvc.Id,
            Protocol = "TCP", Action = "Deny", Direction = "Inbound",
            Description = "DstFWRule"
        };
        _context.FirewallRules.Add(rule);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdAsync(server.Id);

        // Assert
        result.Should().NotBeNull();
        result!.DestinationFirewallRules.Should().HaveCount(1);
        result.DestinationFirewallRules.First().Description.Should().Be("DstFWRule");
    }

    [Test]
    public async Task GetAllAsync_ShouldIncludeServicesLinkedToEachServer()
    {
        // Arrange — two servers, each with one service linked
        var svc1 = new AppService { Name = "Svc-For-Srv1", UserName = "domain\\svc1" };
        var svc2 = new AppService { Name = "Svc-For-Srv2", UserName = "domain\\svc2" };
        _context.AppServices.AddRange(svc1, svc2);
        var srv1 = new Server { Name = "Srv1", IpAddress = "10.0.1.1", OperatingSystem = "Linux", Services = [svc1] };
        var srv2 = new Server { Name = "Srv2", IpAddress = "10.0.1.2", OperatingSystem = "Linux", Services = [svc2] };
        _context.Servers.AddRange(srv1, srv2);
        await _context.SaveChangesAsync();

        // Act
        var servers = (await _repository.GetAllAsync()).ToList();

        // Assert — each server exposes only its own services
        var result1 = servers.First(s => s.Name == "Srv1");
        var result2 = servers.First(s => s.Name == "Srv2");
        result1.Services.Should().HaveCount(1);
        result1.Services.Single().Name.Should().Be("Svc-For-Srv1");
        result2.Services.Should().HaveCount(1);
        result2.Services.Single().Name.Should().Be("Svc-For-Srv2");
    }

    [Test]
    public async Task GetAllAsync_ShouldReturnEmptyServicesCollection_WhenNoServicesLinked()
    {
        // Arrange — server with no linked services
        var svc = new AppService { Name = "UnlinkedSvc", UserName = "domain\\unlinked" };
        _context.AppServices.Add(svc);
        var server = new Server { Name = "Srv-NoSvc", IpAddress = "10.0.2.1", OperatingSystem = "Linux" };
        _context.Servers.Add(server);
        await _context.SaveChangesAsync();

        // Act
        var servers = (await _repository.GetAllAsync()).ToList();

        // Assert — server.Services is empty because the service was not linked to it
        var result = servers.Single(s => s.Name == "Srv-NoSvc");
        result.Services.Should().BeEmpty();
    }

    [Test]
    public async Task GetAllAsync_ShouldNotExposeOtherServersServices()
    {
        // Arrange — one service linked only to server A, not to server B
        var svc = new AppService { Name = "SharedSvc", UserName = "domain\\shared" };
        _context.AppServices.Add(svc);
        var srvA = new Server { Name = "Srv-A", IpAddress = "10.0.3.1", OperatingSystem = "Linux", Services = [svc] };
        var srvB = new Server { Name = "Srv-B", IpAddress = "10.0.3.2", OperatingSystem = "Linux" };
        _context.Servers.AddRange(srvA, srvB);
        await _context.SaveChangesAsync();

        // Act
        var servers = (await _repository.GetAllAsync()).ToList();

        // Assert — srvB has no services
        var resultB = servers.First(s => s.Name == "Srv-B");
        resultB.Services.Should().BeEmpty();
    }
}
