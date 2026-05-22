using FluentAssertions;
using SecurityRule.Domain.Models;
using SecurityRule.Infrastructure.Data;
using SecurityRule.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace SecurityRule.Tests;

[TestFixture]
public class OperatingSystemRepositoryTests
{
    private AppDbContext _context = null!;
    private OperatingSystemRepository _repository = null!;

    [SetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new AppDbContext(options);
        _context.Database.EnsureCreated();
        _repository = new OperatingSystemRepository(_context);
    }

    [TearDown]
    public void TearDown()
    {
        _context.Dispose();
    }

    [Test]
    public async Task GetAllAsync_ShouldReturnAllOptions()
    {
        // Arrange
        _context.OperatingSystemOptions.AddRange(
            new OperatingSystemOption { Name = "Windows 11" },
            new OperatingSystemOption { Name = "Windows Server 2022" },
            new OperatingSystemOption { Name = "Ubuntu 22.04" });
        await _context.SaveChangesAsync();

        // Act
        var result = (await _repository.GetAllAsync()).ToList();

        // Assert
        result.Should().HaveCountGreaterThanOrEqualTo(3);
        result.Select(o => o.Name).Should().Contain("Ubuntu 22.04");
    }

    [Test]
    public async Task GetAllAsync_ShouldReturnItemsInAlphabeticalOrder()
    {
        // Arrange – clear seeded data and add out-of-order entries
        _context.OperatingSystemOptions.RemoveRange(_context.OperatingSystemOptions);
        await _context.SaveChangesAsync();
        _context.OperatingSystemOptions.AddRange(
            new OperatingSystemOption { Name = "Windows Server 2022" },
            new OperatingSystemOption { Name = "Debian 12" },
            new OperatingSystemOption { Name = "Alpine Linux" }
        );
        await _context.SaveChangesAsync();

        // Act
        var result = (await _repository.GetAllAsync()).ToList();

        // Assert
        result.Select(o => o.Name).Should().BeInAscendingOrder();
    }

    [Test]
    public async Task GetAllAsync_ShouldReturnSeededOptions()
    {
        // Arrange
        _context.OperatingSystemOptions.RemoveRange(_context.OperatingSystemOptions);
        await _context.SaveChangesAsync();
        _context.OperatingSystemOptions.AddRange(
            new OperatingSystemOption { Name = "Windows 11" },
            new OperatingSystemOption { Name = "Windows Server 2022" });
        await _context.SaveChangesAsync();

        // Act
        var result = (await _repository.GetAllAsync()).ToList();

        // Assert
        result.Should().HaveCount(2);
        result.Select(o => o.Name).Should().Contain("Windows 11");
        result.Select(o => o.Name).Should().Contain("Windows Server 2022");
    }

    [Test]
    public async Task GetAllAsync_ShouldReturnEmptyList_WhenNoOptions()
    {
        // Arrange – remove all seeded data
        _context.OperatingSystemOptions.RemoveRange(_context.OperatingSystemOptions);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        result.Should().BeEmpty();
    }
}
