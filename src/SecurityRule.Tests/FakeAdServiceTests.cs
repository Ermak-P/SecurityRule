using FluentAssertions;
using SecurityRule.Infrastructure.Services;

namespace SecurityRule.Tests;

[TestFixture]
public class FakeAdServiceTests
{
    private FakeAdService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _service = new FakeAdService();
    }

    [Test]
    public async Task GetUserGroupNamesAsync_ReturnsEmpty_WhenNoRelationsDefined()
    {
        var result = await _service.GetUserGroupNamesAsync("domain\\alice");
        result.Should().BeEmpty();
    }

    [Test]
    public async Task GetUserGroupNamesAsync_ReturnsGroups_AfterAddUserToGroup()
    {
        _service.AddUserToGroup("domain\\alice", "Admins");
        _service.AddUserToGroup("domain\\alice", "Users");

        var result = await _service.GetUserGroupNamesAsync("domain\\alice");

        result.Should().BeEquivalentTo(["Admins", "Users"]);
    }

    [Test]
    public async Task GetGroupMemberUserNamesAsync_ReturnsUsers_AfterAddUserToGroup()
    {
        _service.AddUserToGroup("domain\\alice", "Admins");
        _service.AddUserToGroup("domain\\bob", "Admins");

        var result = await _service.GetGroupMemberUserNamesAsync("Admins");

        result.Should().BeEquivalentTo(["domain\\alice", "domain\\bob"]);
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
        _service.AddUserToGroup("domain\\alice", "Admins");
        _service.AddChildGroup("Root", "Child");

        _service.Reset();

        var groups = await _service.GetUserGroupNamesAsync("domain\\alice");
        var children = await _service.GetGroupChildGroupNamesAsync("Root");

        groups.Should().BeEmpty();
        children.Should().BeEmpty();
    }

    [Test]
    public async Task AddUserToGroup_IsCaseInsensitive_ForLookup()
    {
        _service.AddUserToGroup("domain\\Alice", "Admins");

        var result = await _service.GetUserGroupNamesAsync("domain\\alice");

        result.Should().ContainSingle().Which.Should().Be("Admins");
    }

    [Test]
    public async Task AddUserToGroup_IsBidirectional()
    {
        _service.AddUserToGroup("domain\\alice", "Admins");

        var userGroups = await _service.GetUserGroupNamesAsync("domain\\alice");
        var groupMembers = await _service.GetGroupMemberUserNamesAsync("Admins");

        userGroups.Should().ContainSingle().Which.Should().Be("Admins");
        groupMembers.Should().ContainSingle().Which.Should().Be("domain\\alice");
    }
}
