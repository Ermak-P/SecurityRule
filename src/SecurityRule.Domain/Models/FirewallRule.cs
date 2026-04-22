namespace SecurityRule.Domain.Models;

public class FirewallRule
{
    public int Id { get; set; }
    public string SourceIp { get; set; } = string.Empty;
    public string DestinationIp { get; set; } = string.Empty;
    public int? DestinationPort { get; set; }
    public string Protocol { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Direction { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public string Description { get; set; } = string.Empty;
    public int? ServerId { get; set; }
    public Server? Server { get; set; }
    public int? ServiceId { get; set; }
    public AppService? Service { get; set; }
}
