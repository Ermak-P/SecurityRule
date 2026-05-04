using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SecurityRule.Domain.Models;
using SecurityRule.Infrastructure.Data;
using SecurityRule.Infrastructure.Repositories;

namespace SecurityRule.Tests;

[TestFixture]
public class TagRepositoryTests
{
    private AppDbContext _context = null!;
    private TagRepository _repository = null!;

    [SetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new AppDbContext(options);
        _repository = new TagRepository(_context);
    }

    [TearDown]
    public void TearDown()
    {
        _context.Dispose();
    }

    [Test]
    public async Task GetAllAsync_Returns_Empty_When_No_Tags_Exist()
    {
        var result = await _repository.GetAllAsync();
        result.Should().BeEmpty();
    }

    [Test]
    public async Task GetAllAsync_Returns_All_Tags_Ordered_By_Name()
    {
        _context.Tags.AddRange(
            new Tag { Name = "zebra" },
            new Tag { Name = "alpha" },
            new Tag { Name = "middle" });
        await _context.SaveChangesAsync();

        var result = (await _repository.GetAllAsync()).ToList();

        result.Should().HaveCount(3);
        result.Select(t => t.Name).Should().BeInAscendingOrder();
    }

    [Test]
    public async Task GetOrCreateAsync_Creates_New_Tag_When_Not_Exists()
    {
        var tag = await _repository.GetOrCreateAsync("production");

        tag.Should().NotBeNull();
        tag.Name.Should().Be("production");
        tag.Id.Should().BeGreaterThan(0);

        var inDb = await _context.Tags.SingleAsync(t => t.Name == "production");
        inDb.Id.Should().Be(tag.Id);
    }

    [Test]
    public async Task GetOrCreateAsync_Returns_Existing_Tag_When_Already_Exists()
    {
        var existing = new Tag { Name = "staging" };
        _context.Tags.Add(existing);
        await _context.SaveChangesAsync();

        var result = await _repository.GetOrCreateAsync("staging");

        result.Id.Should().Be(existing.Id);
        var count = await _context.Tags.CountAsync(t => t.Name == "staging");
        count.Should().Be(1);
    }

    [Test]
    public async Task GetOrCreateAsync_Called_Twice_Does_Not_Duplicate_Tag()
    {
        await _repository.GetOrCreateAsync("dev");
        await _repository.GetOrCreateAsync("dev");

        var count = await _context.Tags.CountAsync(t => t.Name == "dev");
        count.Should().Be(1);
    }
}
