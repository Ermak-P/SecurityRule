namespace SecurityRule.Domain.Models;

public class FirewallRule
{
    public int Id { get; set; }
    public string SourceIp { get; set; } = string.Empty;
    public string DestinationIp { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public string Description { get; set; } = string.Empty;
}
