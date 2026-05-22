namespace SecurityRule.Domain.Models;

/// <summary>
/// Represents a locally stored partner name. Only the Name is persisted
/// (the key field), while the Code comes from the external partner service.
/// </summary>
public class PartnerName
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public ICollection<AppService> Services { get; set; } = new List<AppService>();
}
