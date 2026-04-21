using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SecurityRule.Domain.Models;
using SecurityRule.Infrastructure.Data;
using SecurityRule.Infrastructure.Repositories;

namespace SecurityRule.Tests;

[TestFixture]
public class UserRepositoryTests
{
    private AppDbContext _context = null!;
    private UserRepository _repository = null!;

    [SetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new AppDbContext(options);
        _repository = new UserRepository(_context);
    }

    [TearDown]
    public void TearDown()
    {
        _context.Dispose();
    }

    [Test]
    public async Task AddAsync_ShouldAddUser()
    {
        // Arrange
        var user = new User { Name = "domain\\alice", Description = "Test user" };

        // Act
        await _repository.AddAsync(user);

        // Assert
        var result = await _context.Users.ToListAsync();
        result.Should().HaveCount(1);
        result[0].Name.Should().Be("domain\\alice");
    }

    [Test]
    public async Task GetAllAsync_ShouldReturnAllUsers()
    {
        // Arrange
        _context.Users.AddRange(
            new User { Name = "domain\\alice", Description = "Alice" },
            new User { Name = "domain\\bob", Description = "Bob" }
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        result.Should().HaveCount(2);
    }

    [Test]
    public async Task GetByIdAsync_ShouldReturnCorrectUser()
    {
        // Arrange
        var user = new User { Name = "domain\\alice", Description = "Alice" };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdAsync(user.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("domain\\alice");
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
    public async Task GetByIdAsync_ShouldIncludeServicesWithServers()
    {
        // Arrange
        var server = new Server { Name = "Web-01", IpAddress = "10.0.0.1", OperatingSystem = "Linux" };
        var service = new AppService { Name = "WebApp", UserName = "domain\\svc", Servers = [server] };
        var user = new User { Name = "domain\\alice", Services = [service] };
        _context.Servers.Add(server);
        _context.AppServices.Add(service);
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdAsync(user.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Services.Should().HaveCount(1);
        result.Services.First().Servers.Should().HaveCount(1);
        result.Services.First().Servers.First().Name.Should().Be("Web-01");
    }

    [Test]
    public async Task UpdateAsync_ShouldUpdateNameAndDescription()
    {
        // Arrange
        var user = new User { Name = "domain\\alice", Description = "Old description" };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        user.Name = "domain\\alice-updated";
        user.Description = "New description";
        await _repository.UpdateAsync(user);

        // Assert
        var result = await _context.Users.FindAsync(user.Id);
        result!.Name.Should().Be("domain\\alice-updated");
        result.Description.Should().Be("New description");
    }

    [Test]
    public async Task UpdateAsync_ShouldNotThrow_WhenUserNotFound()
    {
        // Arrange
        var nonExistentUser = new User { Id = 999, Name = "ghost" };

        // Act
        var act = async () => await _repository.UpdateAsync(nonExistentUser);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Test]
    public async Task DeleteAsync_ShouldRemoveUser()
    {
        // Arrange
        var user = new User { Name = "domain\\alice" };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        await _repository.DeleteAsync(user.Id);

        // Assert
        var result = await _context.Users.ToListAsync();
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
    public async Task DeleteAsync_ShouldNotDeleteGroups()
    {
        // Arrange – groups are independent entities; deleting a user must not affect groups
        var group = new Group { Name = "Admins" };
        _context.Groups.Add(group);
        var user = new User { Name = "domain\\alice" };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        await _repository.DeleteAsync(user.Id);

        // Assert – the Group entity itself must still exist
        var groups = await _context.Groups.ToListAsync();
        groups.Should().HaveCount(1);
        groups.First().Name.Should().Be("Admins");
    }
}
