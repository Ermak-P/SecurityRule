namespace SecurityRule.Infrastructure.Data.FakeAd;

/// <summary>Represents an AD group stored in the FakeAd database.</summary>
public class AdGroup
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public ICollection<AdUserGroupMembership> UserMemberships { get; set; } = [];

    /// <summary>Groups that this group belongs to (this group is a child of them).</summary>
    public ICollection<AdGroupGroupMembership> ParentMemberships { get; set; } = [];

    /// <summary>Groups that belong to this group (this group is a parent of them).</summary>
    public ICollection<AdGroupGroupMembership> ChildMemberships { get; set; } = [];
}
