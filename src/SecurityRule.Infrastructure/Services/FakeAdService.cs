using Microsoft.EntityFrameworkCore;
using SecurityRule.Domain.Interfaces;
using SecurityRule.Infrastructure.Data;
using SecurityRule.Infrastructure.Data.FakeAd;

namespace SecurityRule.Infrastructure.Services;

/// <summary>
/// Database-backed implementation of <see cref="IAdService"/> used during development and
/// testing when a real Active Directory is not available.
/// All membership data is stored in a dedicated <see cref="FakeAdDbContext"/> (separate
/// from the main SecurityRule database).
/// The mutation helpers <see cref="AddUserToGroup"/>, <see cref="AddChildGroup"/> and
/// <see cref="Reset"/> are provided for test-scenario setup and teardown.
/// </summary>
public class FakeAdService : IAdService
{
    private readonly IDbContextFactory<FakeAdDbContext> _dbFactory;

    public FakeAdService(IDbContextFactory<FakeAdDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    // ── Mutation helpers (for tests / demo data) ──────────────────────────────

    /// <summary>
    /// Declares that <paramref name="userName"/> is a member of <paramref name="groupName"/>.
    /// Creates the user and/or group records if they do not exist.
    /// </summary>
    public void AddUserToGroup(string userName, string groupName)
    {
        using var db = _dbFactory.CreateDbContext();

        var user = db.AdUsers.FirstOrDefault(u => u.Name == userName);
        if (user == null)
        {
            user = new AdUser { Name = userName };
            db.AdUsers.Add(user);
            db.SaveChanges();
        }

        var group = db.AdGroups.FirstOrDefault(g => g.Name == groupName);
        if (group == null)
        {
            group = new AdGroup { Name = groupName };
            db.AdGroups.Add(group);
            db.SaveChanges();
        }

        var exists = db.AdUserGroupMemberships
            .Any(m => m.UserId == user.Id && m.GroupId == group.Id);
        if (!exists)
        {
            db.AdUserGroupMemberships.Add(
                new AdUserGroupMembership { UserId = user.Id, GroupId = group.Id });
            db.SaveChanges();
        }
    }

    /// <summary>
    /// Declares that <paramref name="childGroupName"/> is a child (member) of
    /// <paramref name="parentGroupName"/>.
    /// Creates the group records if they do not exist.
    /// </summary>
    public void AddChildGroup(string parentGroupName, string childGroupName)
    {
        using var db = _dbFactory.CreateDbContext();

        var parent = db.AdGroups.FirstOrDefault(g => g.Name == parentGroupName);
        if (parent == null)
        {
            parent = new AdGroup { Name = parentGroupName };
            db.AdGroups.Add(parent);
            db.SaveChanges();
        }

        var child = db.AdGroups.FirstOrDefault(g => g.Name == childGroupName);
        if (child == null)
        {
            child = new AdGroup { Name = childGroupName };
            db.AdGroups.Add(child);
            db.SaveChanges();
        }

        var exists = db.AdGroupGroupMemberships
            .Any(m => m.ParentGroupId == parent.Id && m.ChildGroupId == child.Id);
        if (!exists)
        {
            db.AdGroupGroupMemberships.Add(
                new AdGroupGroupMembership { ParentGroupId = parent.Id, ChildGroupId = child.Id });
            db.SaveChanges();
        }
    }

    /// <summary>Removes all AD membership data (call between test scenarios).</summary>
    public void Reset()
    {
        using var db = _dbFactory.CreateDbContext();
        db.AdGroupGroupMemberships.RemoveRange(db.AdGroupGroupMemberships);
        db.AdUserGroupMemberships.RemoveRange(db.AdUserGroupMemberships);
        db.AdGroups.RemoveRange(db.AdGroups);
        db.AdUsers.RemoveRange(db.AdUsers);
        db.SaveChanges();
    }

    // ── IAdService ────────────────────────────────────────────────────────────

    public async Task<IEnumerable<string>> GetUserGroupNamesAsync(string userName)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.AdUserGroupMemberships
            .Where(m => m.User.Name == userName)
            .Select(m => m.Group.Name)
            .ToListAsync();
    }

    public async Task<IEnumerable<string>> GetGroupMemberUserNamesAsync(string groupName)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.AdUserGroupMemberships
            .Where(m => m.Group.Name == groupName)
            .Select(m => m.User.Name)
            .ToListAsync();
    }

    public async Task<IEnumerable<string>> GetGroupChildGroupNamesAsync(string groupName)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.AdGroupGroupMemberships
            .Where(m => m.ParentGroup.Name == groupName)
            .Select(m => m.ChildGroup.Name)
            .ToListAsync();
    }

    public async Task<IEnumerable<string>> GetGroupParentGroupNamesAsync(string groupName)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.AdGroupGroupMemberships
            .Where(m => m.ChildGroup.Name == groupName)
            .Select(m => m.ParentGroup.Name)
            .ToListAsync();
    }
}
