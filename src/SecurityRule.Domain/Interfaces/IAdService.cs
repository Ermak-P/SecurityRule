namespace SecurityRule.Domain.Interfaces;

/// <summary>
/// Provides membership data from Active Directory.
/// </summary>
public interface IAdService
{
    /// <summary>Returns the names of AD groups the given user belongs to.</summary>
    Task<IEnumerable<string>> GetUserGroupNamesAsync(string userName);

    /// <summary>Returns the user names that are direct members of the given group.</summary>
    Task<IEnumerable<string>> GetGroupMemberUserNamesAsync(string groupName);

    /// <summary>Returns the names of child groups (groups that belong to the given group).</summary>
    Task<IEnumerable<string>> GetGroupChildGroupNamesAsync(string groupName);

    /// <summary>Returns the names of parent groups (groups that the given group belongs to).</summary>
    Task<IEnumerable<string>> GetGroupParentGroupNamesAsync(string groupName);
}
