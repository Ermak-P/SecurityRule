namespace SecurityRule.Infrastructure.Data.FakeAd;

/// <summary>Join entity: an AD group (child) is a member of another AD group (parent).</summary>
public class AdGroupGroupMembership
{
    public int ParentGroupId { get; set; }
    public AdGroup ParentGroup { get; set; } = null!;

    public int ChildGroupId { get; set; }
    public AdGroup ChildGroup { get; set; } = null!;
}
