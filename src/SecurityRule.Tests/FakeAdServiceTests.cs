using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SecurityRule.Infrastructure.Data;
using SecurityRule.Infrastructure.Services;

namespace SecurityRule.Tests;

[TestFixture]
public class FakeAdServiceTests
{
    private FakeAdService _service = null!;
    private IDbContextFactory<FakeAdDbContext> _factory = null!;

    [SetUp]
    public void SetUp()
    {
        // Each test gets its own isolated in-memory database
        var options = new DbContextOptionsBuilder<FakeAdDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _factory = new TestFakeAdDbContextFactory(options);
        _service = new FakeAdService(_factory);
    }

    [Test]
    public async Task GetUserGroupNamesAsync_ReturnsEmpty_WhenNoRelationsDefined()
    {
        var result = await _service.GetUserGroupNamesAsync("alice");
        result.Should().BeEmpty();
    }

    [Test]
    public async Task GetUserGroupNamesAsync_ReturnsGroups_AfterAddUserToGroup()
    {
        _service.AddUserToGroup("alice", "Admins");
        _service.AddUserToGroup("alice", "Users");

        var result = await _service.GetUserGroupNamesAsync("alice");

        result.Should().BeEquivalentTo(["Admins", "Users"]);
    }

    [Test]
    public async Task GetGroupMemberUserNamesAsync_ReturnsUsers_AfterAddUserToGroup()
    {
        _service.AddUserToGroup("alice", "Admins");
        _service.AddUserToGroup("bob", "Admins");

        var result = await _service.GetGroupMemberUserNamesAsync("Admins");

        result.Should().BeEquivalentTo(["alice", "bob"]);
    }

    [Test]
    public async Task GetGroupChildGroupNamesAsync_ReturnsChildren_AfterAddChildGroup()
    {
        _service.AddChildGroup("Root", "Child1");
        _service.AddChildGroup("Root", "Child2");

        var result = await _service.GetGroupChildGroupNamesAsync("Root");

        result.Should().BeEquivalentTo(["Child1", "Child2"]);
    }

    [Test]
    public async Task GetGroupParentGroupNamesAsync_ReturnsParents_AfterAddChildGroup()
    {
        _service.AddChildGroup("Parent1", "ChildGroup");
        _service.AddChildGroup("Parent2", "ChildGroup");

        var result = await _service.GetGroupParentGroupNamesAsync("ChildGroup");

        result.Should().BeEquivalentTo(["Parent1", "Parent2"]);
    }

    [Test]
    public async Task Reset_ClearsAllData()
    {
        _service.AddUserToGroup("alice", "Admins");
        _service.AddChildGroup("Root", "Child");

        _service.Reset();

        var groups = await _service.GetUserGroupNamesAsync("alice");
        var children = await _service.GetGroupChildGroupNamesAsync("Root");

        groups.Should().BeEmpty();
        children.Should().BeEmpty();
    }

    [Test]
    public async Task AddUserToGroup_IsBidirectional()
    {
        _service.AddUserToGroup("alice", "Admins");

        var userGroups = await _service.GetUserGroupNamesAsync("alice");
        var groupMembers = await _service.GetGroupMemberUserNamesAsync("Admins");

        userGroups.Should().ContainSingle().Which.Should().Be("Admins");
        groupMembers.Should().ContainSingle().Which.Should().Be("alice");
    }

    // ── Helper: test-only IDbContextFactory implementation ────────────────────

    private sealed class TestFakeAdDbContextFactory : IDbContextFactory<FakeAdDbContext>
    {
        private readonly DbContextOptions<FakeAdDbContext> _options;

        public TestFakeAdDbContextFactory(DbContextOptions<FakeAdDbContext> options)
            => _options = options;

        public FakeAdDbContext CreateDbContext() => new(_options);
    }
}
