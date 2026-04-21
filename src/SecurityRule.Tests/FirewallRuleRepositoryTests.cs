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
}
