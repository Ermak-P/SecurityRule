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
        var service = new AppService { Name = "MyService", UserName = "domain\\svc", Servers = [server] };

        // Act
        await _repository.AddAsync(service);

        // Assert
        var result = await _context.AppServices.Include(s => s.Servers).ToListAsync();
        result.Should().HaveCount(1);
        result[0].Name.Should().Be("MyService");
        result[0].Servers.Should().ContainSingle(s => s.Id == server.Id);
    }

    [Test]
    public async Task GetAllAsync_ShouldReturnAllServices()
    {
        // Arrange
        var server = await CreateServerAsync();
        _context.AppServices.AddRange(
            new AppService { Name = "Svc1", UserName = "domain\\svc1", Servers = [server] },
            new AppService { Name = "Svc2", UserName = "domain\\svc2", Servers = [server] }
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
        var service = new AppService { Name = "Svc1", UserName = "domain\\svc1", Servers = [server] };
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
        var service = new AppService { Name = "Svc1", UserName = "domain\\svc1", Servers = [server] };
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
        var service = new AppService { Name = "Svc1", UserName = "domain\\svc1", Servers = [server] };
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
        var service = new AppService { Name = "Svc1", UserName = "domain\\svc1", Servers = [server] };
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
        var service = new AppService { Name = "SharedSvc", UserName = "domain\\shared", Servers = [server1, server2] };

        // Act
        await _repository.AddAsync(service);

        // Assert
        var result = await _repository.GetByIdAsync(service.Id);
        result.Should().NotBeNull();
        result!.Servers.Should().HaveCount(2);
    }

    [Test]
    public async Task UpdateAsync_ShouldAssociateServers()
    {
        // Arrange
        var server1 = new Server { Name = "Server1", IpAddress = "10.0.0.1", OperatingSystem = "Linux" };
        var server2 = new Server { Name = "Server2", IpAddress = "10.0.0.2", OperatingSystem = "Windows" };
        _context.Servers.AddRange(server1, server2);
        var service = new AppService { Name = "Svc1", UserName = "domain\\svc1" };
        _context.AppServices.Add(service);
        await _context.SaveChangesAsync();

        // Act — associate both servers
        service.Servers = [server1, server2];
        await _repository.UpdateAsync(service);

        // Assert
        var updated = await _repository.GetByIdAsync(service.Id);
        updated!.Servers.Should().HaveCount(2);
        updated.Servers.Select(s => s.Name).Should().BeEquivalentTo(["Server1", "Server2"]);
    }

    [Test]
    public async Task UpdateAsync_ShouldReplaceServers()
    {
        // Arrange
        var server1 = new Server { Name = "Server1", IpAddress = "10.0.0.1", OperatingSystem = "Linux" };
        var server2 = new Server { Name = "Server2", IpAddress = "10.0.0.2", OperatingSystem = "Windows" };
        _context.Servers.AddRange(server1, server2);
        var service = new AppService
        {
            Name = "Svc1",
            UserName = "domain\\svc1",
            Servers = [server1]
        };
        _context.AppServices.Add(service);
        await _context.SaveChangesAsync();

        // Act — replace server1 with server2
        service.Servers = [server2];
        await _repository.UpdateAsync(service);

        // Assert
        var updated = await _repository.GetByIdAsync(service.Id);
        updated!.Servers.Should().HaveCount(1);
        updated.Servers.Single().Name.Should().Be("Server2");
    }

    [Test]
    public async Task GetByIdAsync_ShouldIncludeSourceConnections()
    {
        // Arrange
        var srcSrv = new Server { Name = "Src-Server", IpAddress = "10.0.7.1", OperatingSystem = "Linux" };
        var dstSrv = new Server { Name = "Dst-Server", IpAddress = "10.0.7.2", OperatingSystem = "Linux" };
        _context.Servers.AddRange(srcSrv, dstSrv);
        var service = new AppService { Name = "InvoiceSvc", UserName = "domain\\invoice" };
        var dstSvc = new AppService { Name = "Dst-Service", UserName = "domain\\dst" };
        _context.AppServices.AddRange(service, dstSvc);
        await _context.SaveChangesAsync();

        var connection = new ServiceConnection
        {
            SourceServerId       = srcSrv.Id,
            SourceServiceId      = service.Id,
            DestinationServerId  = dstSrv.Id,
            DestinationServiceId = dstSvc.Id,
            Protocol = "TCP"
        };
        _context.ServiceConnections.Add(connection);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdAsync(service.Id);

        // Assert
        result.Should().NotBeNull();
        result!.SourceConnections.Should().HaveCount(1);
        result.SourceConnections.First().Protocol.Should().Be("TCP");
    }

    [Test]
    public async Task GetByIdAsync_ShouldIncludeDestinationConnections()
    {
        // Arrange
        var srcSrv = new Server { Name = "Src-Server", IpAddress = "10.0.6.1", OperatingSystem = "Linux" };
        var dstSrv = new Server { Name = "Dst-Server", IpAddress = "10.0.6.2", OperatingSystem = "Linux" };
        _context.Servers.AddRange(srcSrv, dstSrv);
        var srcSvc = new AppService { Name = "Src-Service", UserName = "domain\\src" };
        var service = new AppService { Name = "DestInvoiceSvc", UserName = "domain\\dstinvoice" };
        _context.AppServices.AddRange(srcSvc, service);
        await _context.SaveChangesAsync();

        var connection = new ServiceConnection
        {
            SourceServerId       = srcSrv.Id,
            SourceServiceId      = srcSvc.Id,
            DestinationServerId  = dstSrv.Id,
            DestinationServiceId = service.Id,
            Protocol = "UDP"
        };
        _context.ServiceConnections.Add(connection);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdAsync(service.Id);

        // Assert
        result.Should().NotBeNull();
        result!.DestinationConnections.Should().HaveCount(1);
        result.DestinationConnections.First().Protocol.Should().Be("UDP");
    }

    [Test]
    public async Task AddAsync_ShouldSaveServiceWithTags()
    {
        // Arrange
        var tag1 = new Tag { Name = "backend" };
        var tag2 = new Tag { Name = "api" };
        _context.Tags.AddRange(tag1, tag2);
        await _context.SaveChangesAsync();

        var service = new AppService
        {
            Name = "TaggedSvc",
            UserName = "domain\\tagged",
            Tags = [tag1, tag2]
        };

        // Act
        await _repository.AddAsync(service);

        // Assert
        var result = await _context.AppServices.Include(s => s.Tags).FirstAsync(s => s.Name == "TaggedSvc");
        result.Tags.Should().HaveCount(2);
        result.Tags.Select(t => t.Name).Should().BeEquivalentTo(["backend", "api"]);
    }

    [Test]
    public async Task GetAllAsync_ShouldIncludeTagsForEachService()
    {
        // Arrange
        var tag = new Tag { Name = "monitoring" };
        _context.Tags.Add(tag);
        var service = new AppService { Name = "MonitorSvc", UserName = "domain\\mon", Tags = [tag] };
        _context.AppServices.Add(service);
        await _context.SaveChangesAsync();

        // Act
        var services = (await _repository.GetAllAsync()).ToList();

        // Assert
        var result = services.Single(s => s.Name == "MonitorSvc");
        result.Tags.Should().HaveCount(1);
        result.Tags.Single().Name.Should().Be("monitoring");
    }

    [Test]
    public async Task UpdateAsync_ShouldReplaceTags()
    {
        // Arrange
        var tagA = new Tag { Name = "svc-tag-a" };
        var tagB = new Tag { Name = "svc-tag-b" };
        _context.Tags.AddRange(tagA, tagB);
        var service = new AppService { Name = "Svc", UserName = "domain\\svc", Tags = [tagA] };
        _context.AppServices.Add(service);
        await _context.SaveChangesAsync();

        // Act — replace tagA with tagB
        service.Tags = [tagB];
        await _repository.UpdateAsync(service);

        // Assert
        var updated = await _repository.GetByIdAsync(service.Id);
        updated!.Tags.Should().HaveCount(1);
        updated.Tags.Single().Name.Should().Be("svc-tag-b");
    }
}
