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

    [Test]
    public async Task AddAsync_ShouldAddRule()
    {
        // Arrange
        var rule = new FirewallRule
        {
            SourceIp = "192.168.1.1",
            DestinationIp = "10.0.0.1",
            ExpiresAt = DateTime.Now.AddYears(1),
            Description = "Test rule"
        };

        // Act
        await _repository.AddAsync(rule);

        // Assert
        var result = await _context.FirewallRules.ToListAsync();
        result.Should().HaveCount(1);
        result[0].SourceIp.Should().Be("192.168.1.1");
    }

    [Test]
    public async Task GetAllAsync_ShouldReturnAllRules()
    {
        // Arrange
        _context.FirewallRules.AddRange(
            new FirewallRule { SourceIp = "10.0.0.1", DestinationIp = "10.0.0.2", ExpiresAt = DateTime.Now.AddYears(1), Description = "Rule1" },
            new FirewallRule { SourceIp = "10.0.0.3", DestinationIp = "10.0.0.4", ExpiresAt = DateTime.Now.AddYears(2), Description = "Rule2" }
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        result.Should().HaveCount(2);
    }

    [Test]
    public async Task GetByIdAsync_ShouldReturnCorrectRule()
    {
        // Arrange
        var rule = new FirewallRule { SourceIp = "10.0.0.1", DestinationIp = "10.0.0.2", ExpiresAt = DateTime.Now.AddYears(1), Description = "Rule1" };
        _context.FirewallRules.Add(rule);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdAsync(rule.Id);

        // Assert
        result.Should().NotBeNull();
        result!.SourceIp.Should().Be("10.0.0.1");
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
    public async Task UpdateAsync_ShouldUpdateRule()
    {
        // Arrange
        var rule = new FirewallRule { SourceIp = "10.0.0.1", DestinationIp = "10.0.0.2", ExpiresAt = DateTime.Now.AddYears(1), Description = "Rule1" };
        _context.FirewallRules.Add(rule);
        await _context.SaveChangesAsync();

        // Act
        rule.Description = "UpdatedRule";
        await _repository.UpdateAsync(rule);

        // Assert
        var result = await _context.FirewallRules.FindAsync(rule.Id);
        result!.Description.Should().Be("UpdatedRule");
    }

    [Test]
    public async Task DeleteAsync_ShouldRemoveRule()
    {
        // Arrange
        var rule = new FirewallRule { SourceIp = "10.0.0.1", DestinationIp = "10.0.0.2", ExpiresAt = DateTime.Now.AddYears(1), Description = "Rule1" };
        _context.FirewallRules.Add(rule);
        await _context.SaveChangesAsync();

        // Act
        await _repository.DeleteAsync(rule.Id);

        // Assert
        var result = await _context.FirewallRules.ToListAsync();
        result.Should().BeEmpty();
    }

    [Test]
    public async Task IsExpired_WhenExpiresAtIsInPast()
    {
        // Arrange
        var rule = new FirewallRule
        {
            SourceIp = "10.0.0.1",
            DestinationIp = "10.0.0.2",
            ExpiresAt = DateTime.Now.AddDays(-1),
            Description = "Expired rule"
        };
        _context.FirewallRules.Add(rule);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdAsync(rule.Id);

        // Assert
        result!.ExpiresAt.Should().BeBefore(DateTime.Now);
    }

    [Test]
    public async Task AddAsync_WithServerId_ShouldLinkToServer()
    {
        // Arrange
        var server = new Server { Name = "GW-01", IpAddress = "10.0.1.1", OperatingSystem = "Linux" };
        _context.Servers.Add(server);
        await _context.SaveChangesAsync();

        var rule = new FirewallRule
        {
            ServerId = server.Id,
            ExpiresAt = DateTime.Now.AddYears(1),
            Description = "Server-linked rule"
        };

        // Act
        await _repository.AddAsync(rule);

        // Assert
        var result = await _repository.GetByIdAsync(rule.Id);
        result.Should().NotBeNull();
        result!.ServerId.Should().Be(server.Id);
        result.Server.Should().NotBeNull();
        result.Server!.Name.Should().Be("GW-01");
    }

    [Test]
    public async Task AddAsync_WithServiceId_ShouldLinkToService()
    {
        // Arrange
        var service = new AppService { Name = "AuthApi", UserName = "domain\\svc" };
        _context.AppServices.Add(service);
        await _context.SaveChangesAsync();

        var rule = new FirewallRule
        {
            ServiceId = service.Id,
            ExpiresAt = DateTime.Now.AddYears(1),
            Description = "Service-linked rule"
        };

        // Act
        await _repository.AddAsync(rule);

        // Assert
        var result = await _repository.GetByIdAsync(rule.Id);
        result.Should().NotBeNull();
        result!.ServiceId.Should().Be(service.Id);
        result.Service.Should().NotBeNull();
        result.Service!.Name.Should().Be("AuthApi");
    }

    [Test]
    public async Task GetByIdAsync_ShouldIncludeServerNavigation()
    {
        // Arrange
        var server = new Server { Name = "DB-01", IpAddress = "10.0.2.1", OperatingSystem = "Windows" };
        _context.Servers.Add(server);
        await _context.SaveChangesAsync();

        var rule = new FirewallRule { ServerId = server.Id, ExpiresAt = DateTime.Now.AddYears(1), Description = "R1" };
        _context.FirewallRules.Add(rule);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdAsync(rule.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Server.Should().NotBeNull();
        result.Server!.Name.Should().Be("DB-01");
        result.Server.IpAddress.Should().Be("10.0.2.1");
    }

    [Test]
    public async Task GetByIdAsync_ShouldIncludeServiceNavigation()
    {
        // Arrange
        var service = new AppService { Name = "PaymentSvc", UserName = "domain\\pay" };
        _context.AppServices.Add(service);
        await _context.SaveChangesAsync();

        var rule = new FirewallRule { ServiceId = service.Id, ExpiresAt = DateTime.Now.AddYears(1), Description = "R2" };
        _context.FirewallRules.Add(rule);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdAsync(rule.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Service.Should().NotBeNull();
        result.Service!.Name.Should().Be("PaymentSvc");
    }

    [Test]
    public async Task GetAllAsync_ShouldIncludeServerNavigation()
    {
        // Arrange
        var server = new Server { Name = "App-01", IpAddress = "10.0.3.1", OperatingSystem = "Linux" };
        _context.Servers.Add(server);
        await _context.SaveChangesAsync();

        _context.FirewallRules.AddRange(
            new FirewallRule { ServerId = server.Id, ExpiresAt = DateTime.Now.AddYears(1), Description = "Rule A" },
            new FirewallRule { SourceIp = "1.2.3.4", ExpiresAt = DateTime.Now.AddYears(1), Description = "Rule B" }
        );
        await _context.SaveChangesAsync();

        // Act
        var result = (await _repository.GetAllAsync()).ToList();

        // Assert
        result.Should().HaveCount(2);
        result.Single(r => r.Description == "Rule A").Server.Should().NotBeNull();
        result.Single(r => r.Description == "Rule B").Server.Should().BeNull();
    }

    [Test]
    public async Task UpdateAsync_ShouldUpdateServerId()
    {
        // Arrange
        var server = new Server { Name = "Proxy-01", IpAddress = "10.0.4.1", OperatingSystem = "Linux" };
        _context.Servers.Add(server);
        var rule = new FirewallRule { ExpiresAt = DateTime.Now.AddYears(1), Description = "NoServer" };
        _context.FirewallRules.Add(rule);
        await _context.SaveChangesAsync();

        // Act — link the rule to the server
        rule.ServerId = server.Id;
        await _repository.UpdateAsync(rule);

        // Assert
        var result = await _repository.GetByIdAsync(rule.Id);
        result!.ServerId.Should().Be(server.Id);
        result.Server.Should().NotBeNull();
        result.Server!.Name.Should().Be("Proxy-01");
    }

    [Test]
    public async Task UpdateAsync_ShouldClearServerId()
    {
        // Arrange
        var server = new Server { Name = "Cache-01", IpAddress = "10.0.5.1", OperatingSystem = "Linux" };
        _context.Servers.Add(server);
        await _context.SaveChangesAsync();

        var rule = new FirewallRule { ServerId = server.Id, ExpiresAt = DateTime.Now.AddYears(1), Description = "Linked" };
        _context.FirewallRules.Add(rule);
        await _context.SaveChangesAsync();

        // Act — unlink from server
        rule.ServerId = null;
        await _repository.UpdateAsync(rule);

        // Assert
        var result = await _context.FirewallRules.FindAsync(rule.Id);
        result!.ServerId.Should().BeNull();
    }

    [Test]
    public async Task UpdateAsync_ShouldUpdateServiceId()
    {
        // Arrange
        var service = new AppService { Name = "MailSvc", UserName = "domain\\mail" };
        _context.AppServices.Add(service);
        var rule = new FirewallRule { ExpiresAt = DateTime.Now.AddYears(1), Description = "NoService" };
        _context.FirewallRules.Add(rule);
        await _context.SaveChangesAsync();

        // Act — link to service
        rule.ServiceId = service.Id;
        await _repository.UpdateAsync(rule);

        // Assert
        var result = await _repository.GetByIdAsync(rule.Id);
        result!.ServiceId.Should().Be(service.Id);
        result.Service.Should().NotBeNull();
        result.Service!.Name.Should().Be("MailSvc");
    }

    [Test]
    public async Task AddAsync_WithNullServerAndService_ShouldStoreManualIp()
    {
        // Arrange — rule with manual IP (no server/service links)
        var rule = new FirewallRule
        {
            SourceIp      = "192.168.0.10",
            DestinationIp = "192.168.0.20",
            DestinationPort = 443,
            Protocol      = "TCP",
            Action        = "Allow",
            Direction     = "Inbound",
            ExpiresAt     = DateTime.Now.AddYears(1),
            Description   = "Manual IP rule"
        };

        // Act
        await _repository.AddAsync(rule);

        // Assert
        var result = await _repository.GetByIdAsync(rule.Id);
        result.Should().NotBeNull();
        result!.SourceIp.Should().Be("192.168.0.10");
        result.DestinationIp.Should().Be("192.168.0.20");
        result.DestinationPort.Should().Be(443);
        result.ServerId.Should().BeNull();
        result.ServiceId.Should().BeNull();
        result.Server.Should().BeNull();
        result.Service.Should().BeNull();
    }
}
