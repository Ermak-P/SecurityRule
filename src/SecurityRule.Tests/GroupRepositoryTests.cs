using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SecurityRule.Domain.Models;
using SecurityRule.Infrastructure.Data;
using SecurityRule.Infrastructure.Repositories;

namespace SecurityRule.Tests;

[TestFixture]
public class GroupRepositoryTests
{
    private AppDbContext _context = null!;
    private GroupRepository _repository = null!;

    [SetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new AppDbContext(options);
        _repository = new GroupRepository(_context);
    }

    [TearDown]
    public void TearDown()
    {
        _context.Dispose();
    }

    [Test]
    public async Task AddAsync_ShouldAddGroup()
    {
        var group = new Group { Name = "Admins", Description = "Admin group" };

        await _repository.AddAsync(group);

        var result = await _context.Groups.ToListAsync();
        result.Should().HaveCount(1);
        result[0].Name.Should().Be("Admins");
    }

    [Test]
    public async Task GetAllAsync_ShouldReturnAllGroups()
    {
        _context.Groups.AddRange(
            new Group { Name = "Admins" },
            new Group { Name = "Users" }
        );
        await _context.SaveChangesAsync();

        var result = await _repository.GetAllAsync();

        result.Should().HaveCount(2);
    }

    [Test]
    public async Task GetByIdAsync_ShouldReturnCorrectGroup()
    {
        var group = new Group { Name = "DevTeam", Description = "Developers" };
        _context.Groups.Add(group);
        await _context.SaveChangesAsync();

        var result = await _repository.GetByIdAsync(group.Id);

        result.Should().NotBeNull();
        result!.Name.Should().Be("DevTeam");
        result.Description.Should().Be("Developers");
    }

    [Test]
    public async Task GetByIdAsync_ShouldReturnNull_WhenNotFound()
    {
        var result = await _repository.GetByIdAsync(999);
        result.Should().BeNull();
    }

    [Test]
    public async Task UpdateAsync_ShouldUpdateNameAndDescription()
    {
        var group = new Group { Name = "OldName", Description = "Old description" };
        _context.Groups.Add(group);
        await _context.SaveChangesAsync();

        group.Name = "NewName";
        group.Description = "New description";
        await _repository.UpdateAsync(group);

        var result = await _context.Groups.FindAsync(group.Id);
        result!.Name.Should().Be("NewName");
        result.Description.Should().Be("New description");
    }

    [Test]
    public async Task UpdateAsync_ShouldNotThrow_WhenGroupNotFound()
    {
        var nonExistent = new Group { Id = 999, Name = "Ghost" };

        var act = async () => await _repository.UpdateAsync(nonExistent);

        await act.Should().NotThrowAsync();
    }

    [Test]
    public async Task DeleteAsync_ShouldRemoveGroup()
    {
        var group = new Group { Name = "TempGroup" };
        _context.Groups.Add(group);
        await _context.SaveChangesAsync();

        await _repository.DeleteAsync(group.Id);

        var result = await _context.Groups.ToListAsync();
        result.Should().BeEmpty();
    }

    [Test]
    public async Task DeleteAsync_ShouldNotThrow_WhenNotFound()
    {
        var act = async () => await _repository.DeleteAsync(999);

        await act.Should().NotThrowAsync();
    }
}
