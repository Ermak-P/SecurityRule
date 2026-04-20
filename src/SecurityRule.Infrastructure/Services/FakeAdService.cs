using SecurityRule.Domain.Interfaces;

namespace SecurityRule.Infrastructure.Services;

/// <summary>
/// In-memory implementation of <see cref="IAdService"/> used during development and testing
/// when a real Active Directory is not available. The service is thread-safe and its state
/// can be manipulated via <see cref="AddUserToGroup"/>, <see cref="AddChildGroup"/>, and
/// <see cref="Reset"/> to support scenario-level test isolation.
/// </summary>
public class FakeAdService : IAdService
{
    private readonly object _lock = new();

    // userName (case-insensitive) → set of group names
    private readonly Dictionary<string, HashSet<string>> _userGroups =
        new(StringComparer.OrdinalIgnoreCase);

    // groupName (case-insensitive) → set of member user names
    private readonly Dictionary<string, HashSet<string>> _groupUsers =
        new(StringComparer.OrdinalIgnoreCase);

    // groupName (case-insensitive) → set of child group names
    private readonly Dictionary<string, HashSet<string>> _groupChildren =
        new(StringComparer.OrdinalIgnoreCase);

    // groupName (case-insensitive) → set of parent group names
    private readonly Dictionary<string, HashSet<string>> _groupParents =
        new(StringComparer.OrdinalIgnoreCase);

    // ── Mutation helpers (for tests / demo data) ──────────────────────────────

    /// <summary>
    /// Declares that <paramref name="userName"/> is a member of <paramref name="groupName"/>.
    /// Updates both the user→groups and group→users indexes.
    /// </summary>
    public void AddUserToGroup(string userName, string groupName)
    {
        lock (_lock)
        {
            GetOrAdd(_userGroups, userName).Add(groupName);
            GetOrAdd(_groupUsers, groupName).Add(userName);
        }
    }

    /// <summary>
    /// Declares that <paramref name="childGroupName"/> is a child (member) of
    /// <paramref name="parentGroupName"/>. Updates both parent→children and
    /// child→parents indexes.
    /// </summary>
    public void AddChildGroup(string parentGroupName, string childGroupName)
    {
        lock (_lock)
        {
            GetOrAdd(_groupChildren, parentGroupName).Add(childGroupName);
            GetOrAdd(_groupParents, childGroupName).Add(parentGroupName);
        }
    }

    /// <summary>Removes all AD membership data (call between test scenarios).</summary>
    public void Reset()
    {
        lock (_lock)
        {
            _userGroups.Clear();
            _groupUsers.Clear();
            _groupChildren.Clear();
            _groupParents.Clear();
        }
    }

    // ── IAdService ────────────────────────────────────────────────────────────

    public Task<IEnumerable<string>> GetUserGroupNamesAsync(string userName)
    {
        lock (_lock)
        {
            var result = _userGroups.TryGetValue(userName, out var groups)
                ? (IEnumerable<string>)groups.ToList()
                : [];
            return Task.FromResult(result);
        }
    }

    public Task<IEnumerable<string>> GetGroupMemberUserNamesAsync(string groupName)
    {
        lock (_lock)
        {
            var result = _groupUsers.TryGetValue(groupName, out var users)
                ? (IEnumerable<string>)users.ToList()
                : [];
            return Task.FromResult(result);
        }
    }

    public Task<IEnumerable<string>> GetGroupChildGroupNamesAsync(string groupName)
    {
        lock (_lock)
        {
            var result = _groupChildren.TryGetValue(groupName, out var children)
                ? (IEnumerable<string>)children.ToList()
                : [];
            return Task.FromResult(result);
        }
    }

    public Task<IEnumerable<string>> GetGroupParentGroupNamesAsync(string groupName)
    {
        lock (_lock)
        {
            var result = _groupParents.TryGetValue(groupName, out var parents)
                ? (IEnumerable<string>)parents.ToList()
                : [];
            return Task.FromResult(result);
        }
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private static HashSet<string> GetOrAdd(
        Dictionary<string, HashSet<string>> dict,
        string key)
    {
        if (!dict.TryGetValue(key, out var set))
        {
            set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            dict[key] = set;
        }
        return set;
    }
}
