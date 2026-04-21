namespace SecurityRule.Infrastructure.Data.FakeAd;

/// <summary>Represents a user account stored in the FakeAd database.</summary>
public class AdUser
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public ICollection<AdUserGroupMembership> GroupMemberships { get; set; } = [];
}
