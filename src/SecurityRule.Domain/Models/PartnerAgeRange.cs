namespace SecurityRule.Domain.Models;

/// <summary>
/// An age range (from–to) assigned to a specific partner for a service.
/// </summary>
public class PartnerAgeRange
{
    public int Id { get; set; }
    public int AppServiceId { get; set; }
    public AppService AppService { get; set; } = null!;

    /// <summary>Partner name (denormalised string key matching <see cref="PartnerName.Name"/>).</summary>
    public string PartnerName { get; set; } = string.Empty;

    public int AgeFrom { get; set; }
    public int AgeTo { get; set; }
}
