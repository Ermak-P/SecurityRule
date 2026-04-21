namespace SecurityRule.Infrastructure.Data.FakeAd;

/// <summary>Join entity: an AD user is a member of an AD group.</summary>
public class AdUserGroupMembership
{
    public int UserId { get; set; }
    public AdUser User { get; set; } = null!;

    public int GroupId { get; set; }
    public AdGroup Group { get; set; } = null!;
}
