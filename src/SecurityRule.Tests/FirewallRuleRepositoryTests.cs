using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
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
        var rule = new FirewallRule
        {
            SourceIp = "192.168.1.1",
            DestinationIp = "10.0.0.1",
            ExpiresAt = DateTime.Now.AddYears(1),
            Description = "Test rule"
        };

        await _repository.AddAsync(rule);

        var result = await _context.FirewallRules.ToListAsync();
        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].SourceIp, Is.EqualTo("192.168.1.1"));
    }

    [Test]
    public async Task GetAllAsync_ShouldReturnAllRules()
    {
        _context.FirewallRules.AddRange(
            new FirewallRule { SourceIp = "10.0.0.1", DestinationIp = "10.0.0.2", ExpiresAt = DateTime.Now.AddYears(1), Description = "Rule1" },
            new FirewallRule { SourceIp = "10.0.0.3", DestinationIp = "10.0.0.4", ExpiresAt = DateTime.Now.AddYears(2), Description = "Rule2" }
        );
        await _context.SaveChangesAsync();

        var result = await _repository.GetAllAsync();

        Assert.That(result.Count(), Is.EqualTo(2));
    }

    [Test]
    public async Task GetByIdAsync_ShouldReturnCorrectRule()
    {
        var rule = new FirewallRule { SourceIp = "10.0.0.1", DestinationIp = "10.0.0.2", ExpiresAt = DateTime.Now.AddYears(1), Description = "Rule1" };
        _context.FirewallRules.Add(rule);
        await _context.SaveChangesAsync();

        var result = await _repository.GetByIdAsync(rule.Id);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.SourceIp, Is.EqualTo("10.0.0.1"));
    }

    [Test]
    public async Task GetByIdAsync_ShouldReturnNull_WhenNotFound()
    {
        var result = await _repository.GetByIdAsync(999);

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task UpdateAsync_ShouldUpdateRule()
    {
        var rule = new FirewallRule { SourceIp = "10.0.0.1", DestinationIp = "10.0.0.2", ExpiresAt = DateTime.Now.AddYears(1), Description = "Rule1" };
        _context.FirewallRules.Add(rule);
        await _context.SaveChangesAsync();

        rule.Description = "UpdatedRule";
        await _repository.UpdateAsync(rule);

        var result = await _context.FirewallRules.FindAsync(rule.Id);
        Assert.That(result!.Description, Is.EqualTo("UpdatedRule"));
    }

    [Test]
    public async Task DeleteAsync_ShouldRemoveRule()
    {
        var rule = new FirewallRule { SourceIp = "10.0.0.1", DestinationIp = "10.0.0.2", ExpiresAt = DateTime.Now.AddYears(1), Description = "Rule1" };
        _context.FirewallRules.Add(rule);
        await _context.SaveChangesAsync();

        await _repository.DeleteAsync(rule.Id);

        var result = await _context.FirewallRules.ToListAsync();
        Assert.That(result, Is.Empty);
    }

    [Test]
    public async Task IsExpired_WhenExpiresAtIsInPast()
    {
        var rule = new FirewallRule
        {
            SourceIp = "10.0.0.1",
            DestinationIp = "10.0.0.2",
            ExpiresAt = DateTime.Now.AddDays(-1),
            Description = "Expired rule"
        };
        _context.FirewallRules.Add(rule);
        await _context.SaveChangesAsync();

        var result = await _repository.GetByIdAsync(rule.Id);

        Assert.That(result!.ExpiresAt, Is.LessThan(DateTime.Now));
    }
}
