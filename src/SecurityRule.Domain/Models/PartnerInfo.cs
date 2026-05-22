namespace SecurityRule.Domain.Models;

/// <summary>
/// Data transfer object representing a partner returned by the external partner service.
/// </summary>
public class PartnerInfo
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}
