using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SecurityRule.Domain.Models;
using SecurityRule.Infrastructure.Data;
using SecurityRule.Infrastructure.Repositories;

namespace SecurityRule.Tests;

[TestFixture]
public class FirewallRuleRepositoryTests
{
    private AppDbContext _context = null!;
    private FirewallRuleRepository _repository = null!;

    [SetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new AppDbContext(options);
        _repository = new FirewallRuleRepository(_context);
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

    private FirewallRule BuildRule(Server srcSrv, AppService srcSvc, Server dstSrv, AppService dstSvc,
        string description = "Test rule", DateTime? expiresAt = null)
        => new FirewallRule
        {
            SourceServerId       = srcSrv.Id,
            SourceServiceId      = srcSvc.Id,
            DestinationServerId  = dstSrv.Id,
            DestinationServiceId = dstSvc.Id,
            Protocol    = "TCP",
            Action      = "Allow",
            Direction   = "Inbound",
            ExpiresAt   = expiresAt,
            Description = description
        };

    // ── Tests ─────────────────────────────────────────────────────────────────

    [Test]
    public async Task AddAsync_ShouldAddRule()
    {
        // Arrange
        var (srcSrv, srcSvc, dstSrv, dstSvc) = await SeedEntitiesAsync();
        var rule = BuildRule(srcSrv, srcSvc, dstSrv, dstSvc, "AddTest");

        // Act
        await _repository.AddAsync(rule);

        // Assert
        var stored = await _context.FirewallRules.ToListAsync();
        stored.Should().HaveCount(1);
        stored[0].Description.Should().Be("AddTest");
        stored[0].SourceServerId.Should().Be(srcSrv.Id);
        stored[0].SourceServiceId.Should().Be(srcSvc.Id);
        stored[0].DestinationServerId.Should().Be(dstSrv.Id);
        stored[0].DestinationServiceId.Should().Be(dstSvc.Id);
    }

    [Test]
    public async Task GetAllAsync_ShouldReturnAllRules()
    {
        // Arrange
        var (srcSrv, srcSvc, dstSrv, dstSvc) = await SeedEntitiesAsync();
        _context.FirewallRules.AddRange(
            BuildRule(srcSrv, srcSvc, dstSrv, dstSvc, "Rule1"),
            BuildRule(srcSrv, srcSvc, dstSrv, dstSvc, "Rule2")
        );
        await _context.SaveChangesAsync();

        // Act
        var result = (await _repository.GetAllAsync()).ToList();

        // Assert
        result.Should().HaveCount(2);
    }

    [Test]
    public async Task GetByIdAsync_ShouldReturnCorrectRule()
    {
        // Arrange
        var (srcSrv, srcSvc, dstSrv, dstSvc) = await SeedEntitiesAsync();
        var rule = BuildRule(srcSrv, srcSvc, dstSrv, dstSvc, "FindMe");
        _context.FirewallRules.Add(rule);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdAsync(rule.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Description.Should().Be("FindMe");
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
        var rule = BuildRule(srcSrv, srcSvc, dstSrv, dstSvc, "NavTest");
        _context.FirewallRules.Add(rule);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdAsync(rule.Id);

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
        _context.FirewallRules.Add(BuildRule(srcSrv, srcSvc, dstSrv, dstSvc, "NavAll"));
        await _context.SaveChangesAsync();

        // Act
        var result = (await _repository.GetAllAsync()).ToList();

        // Assert
        result.Should().HaveCount(1);
        var r = result[0];
        r.SourceServer.Should().NotBeNull();
        r.SourceService.Should().NotBeNull();
        r.DestinationServer.Should().NotBeNull();
        r.DestinationService.Should().NotBeNull();
    }

    [Test]
    public async Task UpdateAsync_ShouldUpdateDescription()
    {
        // Arrange
        var (srcSrv, srcSvc, dstSrv, dstSvc) = await SeedEntitiesAsync();
        var rule = BuildRule(srcSrv, srcSvc, dstSrv, dstSvc, "OldDesc");
        _context.FirewallRules.Add(rule);
        await _context.SaveChangesAsync();

        // Act
        rule.Description = "NewDesc";
        await _repository.UpdateAsync(rule);

        // Assert
        var result = await _context.FirewallRules.FindAsync(rule.Id);
        result!.Description.Should().Be("NewDesc");
    }

    [Test]
    public async Task UpdateAsync_ShouldChangeSourceServer()
    {
        // Arrange
        var (srcSrv, srcSvc, dstSrv, dstSvc) = await SeedEntitiesAsync();
        var newSrcSrv = new Server { Name = "NewSrc", IpAddress = "10.0.9.1", OperatingSystem = "Linux" };
        _context.Servers.Add(newSrcSrv);
        await _context.SaveChangesAsync();

        var rule = BuildRule(srcSrv, srcSvc, dstSrv, dstSvc, "ChangeSrc");
        _context.FirewallRules.Add(rule);
        await _context.SaveChangesAsync();

        // Act
        rule.SourceServerId = newSrcSrv.Id;
        await _repository.UpdateAsync(rule);

        // Assert
        var result = await _repository.GetByIdAsync(rule.Id);
        result!.SourceServerId.Should().Be(newSrcSrv.Id);
        result.SourceServer!.Name.Should().Be("NewSrc");
    }

    [Test]
    public async Task UpdateAsync_ShouldChangeDestinationService()
    {
        // Arrange
        var (srcSrv, srcSvc, dstSrv, dstSvc) = await SeedEntitiesAsync();
        var newDstSvc = new AppService { Name = "NewDstSvc", UserName = "domain\\new" };
        _context.AppServices.Add(newDstSvc);
        await _context.SaveChangesAsync();

        var rule = BuildRule(srcSrv, srcSvc, dstSrv, dstSvc, "ChangeDst");
        _context.FirewallRules.Add(rule);
        await _context.SaveChangesAsync();

        // Act
        rule.DestinationServiceId = newDstSvc.Id;
        await _repository.UpdateAsync(rule);

        // Assert
        var result = await _repository.GetByIdAsync(rule.Id);
        result!.DestinationServiceId.Should().Be(newDstSvc.Id);
        result.DestinationService!.Name.Should().Be("NewDstSvc");
    }

    [Test]
    public async Task UpdateAsync_ShouldUpdateExpiresAt_ToNull()
    {
        // Arrange
        var (srcSrv, srcSvc, dstSrv, dstSvc) = await SeedEntitiesAsync();
        var rule = BuildRule(srcSrv, srcSvc, dstSrv, dstSvc, "Expiring", expiresAt: DateTime.Now.AddYears(1));
        _context.FirewallRules.Add(rule);
        await _context.SaveChangesAsync();

        // Act — set unlimited
        rule.ExpiresAt = null;
        await _repository.UpdateAsync(rule);

        // Assert
        var result = await _context.FirewallRules.FindAsync(rule.Id);
        result!.ExpiresAt.Should().BeNull();
    }

    [Test]
    public async Task AddAsync_WithNullExpiresAt_ShouldPersist_AsUnlimited()
    {
        // Arrange
        var (srcSrv, srcSvc, dstSrv, dstSvc) = await SeedEntitiesAsync();
        var rule = BuildRule(srcSrv, srcSvc, dstSrv, dstSvc, "Unlimited", expiresAt: null);

        // Act
        await _repository.AddAsync(rule);

        // Assert
        var result = await _repository.GetByIdAsync(rule.Id);
        result.Should().NotBeNull();
        result!.ExpiresAt.Should().BeNull();
    }

    [Test]
    public async Task AddAsync_WithExpiresAtInPast_ShouldStoreExpiredRule()
    {
        // Arrange
        var (srcSrv, srcSvc, dstSrv, dstSvc) = await SeedEntitiesAsync();
        var pastDate = DateTime.Now.AddDays(-1);
        var rule = BuildRule(srcSrv, srcSvc, dstSrv, dstSvc, "Expired", expiresAt: pastDate);

        // Act
        await _repository.AddAsync(rule);

        // Assert
        var result = await _repository.GetByIdAsync(rule.Id);
        result!.ExpiresAt.Should().BeBefore(DateTime.Now);
    }

    [Test]
    public async Task DeleteAsync_ShouldRemoveRule()
    {
        // Arrange
        var (srcSrv, srcSvc, dstSrv, dstSvc) = await SeedEntitiesAsync();
        var rule = BuildRule(srcSrv, srcSvc, dstSrv, dstSvc, "ToDelete");
        _context.FirewallRules.Add(rule);
        await _context.SaveChangesAsync();

        // Act
        await _repository.DeleteAsync(rule.Id);

        // Assert
        var result = await _context.FirewallRules.ToListAsync();
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
    public async Task UpdateAsync_ShouldUpdateProtocolActionDirection()
    {
        // Arrange
        var (srcSrv, srcSvc, dstSrv, dstSvc) = await SeedEntitiesAsync();
        var rule = BuildRule(srcSrv, srcSvc, dstSrv, dstSvc, "Protocol");
        _context.FirewallRules.Add(rule);
        await _context.SaveChangesAsync();

        // Act
        rule.Protocol  = "UDP";
        rule.Action    = "Deny";
        rule.Direction = "Outbound";
        await _repository.UpdateAsync(rule);

        // Assert
        var result = await _context.FirewallRules.FindAsync(rule.Id);
        result!.Protocol.Should().Be("UDP");
        result.Action.Should().Be("Deny");
        result.Direction.Should().Be("Outbound");
    }
}
