using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SecurityRule.Domain.Models;
using SecurityRule.Infrastructure.Data;
using SecurityRule.Infrastructure.Repositories;

namespace SecurityRule.Tests;

[TestFixture]
public class ServiceConnectionRepositoryTests
{
    private AppDbContext _context = null!;
    private ServiceConnectionRepository _repository = null!;

    [SetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new AppDbContext(options);
        _repository = new ServiceConnectionRepository(_context);
    }

    [TearDown]
    public void TearDown()
    {
        _context.Dispose();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<(Server srcSrv, AppService srcSvc, Server dstSrv, AppService dstSvc)> SeedEntitiesAsync(
        string srcSrvName = "Src-Server", string srcSvcName = "Src-Service",
        string dstSrvName = "Dst-Server", string dstSvcName = "Dst-Service")
    {
        var srcSrv = new Server { Name = srcSrvName, IpAddress = "10.0.0.1", OperatingSystem = "Linux" };
        var srcSvc = new AppService { Name = srcSvcName, UserName = "domain\\src" };
        var dstSrv = new Server { Name = dstSrvName, IpAddress = "10.0.0.2", OperatingSystem = "Linux" };
        var dstSvc = new AppService { Name = dstSvcName, UserName = "domain\\dst" };
        _context.Servers.AddRange(srcSrv, dstSrv);
        _context.AppServices.AddRange(srcSvc, dstSvc);
        await _context.SaveChangesAsync();
        return (srcSrv, srcSvc, dstSrv, dstSvc);
    }

    private ServiceConnection BuildConnection(
        Server? srcSrv, AppService? srcSvc, Server? dstSrv, AppService dstSvc,
        string protocol = "TCP", string description = "")
        => new ServiceConnection
        {
            SourceServerId       = srcSrv?.Id,
            SourceServiceId      = srcSvc?.Id,
            DestinationServerId  = dstSrv?.Id,
            DestinationServiceId = dstSvc.Id,
            Protocol    = protocol,
            Description = description
        };

    // ── Tests ─────────────────────────────────────────────────────────────────

    [Test]
    public async Task AddAsync_ShouldAddConnection()
    {
        // Arrange
        var (srcSrv, srcSvc, dstSrv, dstSvc) = await SeedEntitiesAsync();
        var connection = BuildConnection(srcSrv, srcSvc, dstSrv, dstSvc, "TCP", "Test connection");

        // Act
        await _repository.AddAsync(connection);

        // Assert
        var stored = await _context.ServiceConnections.ToListAsync();
        stored.Should().HaveCount(1);
        stored[0].SourceServerId.Should().Be(srcSrv.Id);
        stored[0].SourceServiceId.Should().Be(srcSvc.Id);
        stored[0].DestinationServerId.Should().Be(dstSrv.Id);
        stored[0].DestinationServiceId.Should().Be(dstSvc.Id);
        stored[0].Protocol.Should().Be("TCP");
        stored[0].Description.Should().Be("Test connection");
    }

    [Test]
    public async Task AddAsync_WithNullSourceServer_ShouldPersist()
    {
        // Arrange
        var (_, srcSvc, dstSrv, dstSvc) = await SeedEntitiesAsync();
        var connection = BuildConnection(null, srcSvc, dstSrv, dstSvc);

        // Act
        await _repository.AddAsync(connection);

        // Assert
        var stored = await _context.ServiceConnections.FindAsync(connection.Id);
        stored!.SourceServerId.Should().BeNull();
        stored.SourceServiceId.Should().Be(srcSvc.Id);
    }

    [Test]
    public async Task AddAsync_WithNullSourceServiceAndServer_ShouldPersist()
    {
        // Arrange
        var (_, _, dstSrv, dstSvc) = await SeedEntitiesAsync();
        var connection = BuildConnection(null, null, dstSrv, dstSvc);

        // Act
        await _repository.AddAsync(connection);

        // Assert
        var stored = await _context.ServiceConnections.FindAsync(connection.Id);
        stored!.SourceServerId.Should().BeNull();
        stored.SourceServiceId.Should().BeNull();
        stored.DestinationServiceId.Should().Be(dstSvc.Id);
    }

    [Test]
    public async Task GetAllAsync_ShouldReturnAllConnections()
    {
        // Arrange
        var (srcSrv, srcSvc, dstSrv, dstSvc) = await SeedEntitiesAsync();
        _context.ServiceConnections.AddRange(
            BuildConnection(srcSrv, srcSvc, dstSrv, dstSvc),
            BuildConnection(srcSrv, srcSvc, dstSrv, dstSvc)
        );
        await _context.SaveChangesAsync();

        // Act
        var result = (await _repository.GetAllAsync()).ToList();

        // Assert
        result.Should().HaveCount(2);
    }

    [Test]
    public async Task GetByIdAsync_ShouldReturnCorrectConnection()
    {
        // Arrange
        var (srcSrv, srcSvc, dstSrv, dstSvc) = await SeedEntitiesAsync();
        var connection = BuildConnection(srcSrv, srcSvc, dstSrv, dstSvc, "UDP", "Test desc");
        _context.ServiceConnections.Add(connection);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdAsync(connection.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Protocol.Should().Be("UDP");
        result.Description.Should().Be("Test desc");
    }

    [Test]
    public async Task GetByIdAsync_ShouldReturnNull_WhenNotFound()
    {
        // Act
        var result = await _repository.GetByIdAsync(999);

        // Assert
        result.Should().BeNull();
    }

    [Test]
    public async Task GetByIdAsync_ShouldIncludeAllNavigationProperties()
    {
        // Arrange
        var (srcSrv, srcSvc, dstSrv, dstSvc) = await SeedEntitiesAsync("SrcS", "SrcSvc", "DstS", "DstSvc");
        var connection = BuildConnection(srcSrv, srcSvc, dstSrv, dstSvc);
        _context.ServiceConnections.Add(connection);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdAsync(connection.Id);

        // Assert
        result.Should().NotBeNull();
        result!.SourceServer.Should().NotBeNull();
        result.SourceServer!.Name.Should().Be("SrcS");
        result.SourceService.Should().NotBeNull();
        result.SourceService!.Name.Should().Be("SrcSvc");
        result.DestinationServer.Should().NotBeNull();
        result.DestinationServer!.Name.Should().Be("DstS");
        result.DestinationService.Should().NotBeNull();
        result.DestinationService!.Name.Should().Be("DstSvc");
    }

    [Test]
    public async Task GetAllAsync_ShouldIncludeAllNavigationProperties()
    {
        // Arrange
        var (srcSrv, srcSvc, dstSrv, dstSvc) = await SeedEntitiesAsync();
        _context.ServiceConnections.Add(BuildConnection(srcSrv, srcSvc, dstSrv, dstSvc));
        await _context.SaveChangesAsync();

        // Act
        var result = (await _repository.GetAllAsync()).ToList();

        // Assert
        result.Should().HaveCount(1);
        var c = result[0];
        c.SourceServer.Should().NotBeNull();
        c.SourceService.Should().NotBeNull();
        c.DestinationServer.Should().NotBeNull();
        c.DestinationService.Should().NotBeNull();
    }

    [Test]
    public async Task UpdateAsync_ShouldUpdateProtocolAndDescription()
    {
        // Arrange
        var (srcSrv, srcSvc, dstSrv, dstSvc) = await SeedEntitiesAsync();
        var connection = BuildConnection(srcSrv, srcSvc, dstSrv, dstSvc, "TCP", "Initial");
        _context.ServiceConnections.Add(connection);
        await _context.SaveChangesAsync();

        // Act
        connection.Protocol    = "UDP";
        connection.Description = "Updated";
        await _repository.UpdateAsync(connection);

        // Assert
        var result = await _context.ServiceConnections.FindAsync(connection.Id);
        result!.Protocol.Should().Be("UDP");
        result.Description.Should().Be("Updated");
    }

    [Test]
    public async Task UpdateAsync_ShouldChangeSourceServer()
    {
        // Arrange
        var (srcSrv, srcSvc, dstSrv, dstSvc) = await SeedEntitiesAsync();
        var newSrcSrv = new Server { Name = "NewSrc", IpAddress = "10.0.9.1", OperatingSystem = "Linux" };
        _context.Servers.Add(newSrcSrv);
        await _context.SaveChangesAsync();

        var connection = BuildConnection(srcSrv, srcSvc, dstSrv, dstSvc);
        _context.ServiceConnections.Add(connection);
        await _context.SaveChangesAsync();

        // Act
        connection.SourceServerId = newSrcSrv.Id;
        await _repository.UpdateAsync(connection);

        // Assert
        var result = await _repository.GetByIdAsync(connection.Id);
        result!.SourceServerId.Should().Be(newSrcSrv.Id);
        result.SourceServer!.Name.Should().Be("NewSrc");
    }

    [Test]
    public async Task UpdateAsync_ShouldSetSourceServerToNull()
    {
        // Arrange
        var (srcSrv, srcSvc, dstSrv, dstSvc) = await SeedEntitiesAsync();
        var connection = BuildConnection(srcSrv, srcSvc, dstSrv, dstSvc);
        _context.ServiceConnections.Add(connection);
        await _context.SaveChangesAsync();

        // Act
        connection.SourceServerId = null;
        await _repository.UpdateAsync(connection);

        // Assert
        var result = await _context.ServiceConnections.FindAsync(connection.Id);
        result!.SourceServerId.Should().BeNull();
    }

    [Test]
    public async Task UpdateAsync_ShouldUpdateDescriptionToEmpty()
    {
        // Arrange
        var (srcSrv, srcSvc, dstSrv, dstSvc) = await SeedEntitiesAsync();
        var connection = BuildConnection(srcSrv, srcSvc, dstSrv, dstSvc, "TCP", "Some description");
        _context.ServiceConnections.Add(connection);
        await _context.SaveChangesAsync();

        // Act
        connection.Description = string.Empty;
        await _repository.UpdateAsync(connection);

        // Assert
        var result = await _context.ServiceConnections.FindAsync(connection.Id);
        result!.Description.Should().BeEmpty();
    }

    [Test]
    public async Task DeleteAsync_ShouldRemoveConnection()
    {
        // Arrange
        var (srcSrv, srcSvc, dstSrv, dstSvc) = await SeedEntitiesAsync();
        var connection = BuildConnection(srcSrv, srcSvc, dstSrv, dstSvc);
        _context.ServiceConnections.Add(connection);
        await _context.SaveChangesAsync();

        // Act
        await _repository.DeleteAsync(connection.Id);

        // Assert
        var result = await _context.ServiceConnections.ToListAsync();
        result.Should().BeEmpty();
    }

    [Test]
    public async Task DeleteAsync_ShouldNotThrow_WhenNotFound()
    {
        // Act
        var act = async () => await _repository.DeleteAsync(999);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Test]
    public async Task AddAsync_WithNullDestinationServer_ShouldPersist()
    {
        // Arrange
        var (srcSrv, srcSvc, _, dstSvc) = await SeedEntitiesAsync();
        var connection = BuildConnection(srcSrv, srcSvc, null, dstSvc);

        // Act
        await _repository.AddAsync(connection);

        // Assert
        var stored = await _context.ServiceConnections.FindAsync(connection.Id);
        stored!.DestinationServerId.Should().BeNull();
        stored.DestinationServiceId.Should().Be(dstSvc.Id);
    }
}
