namespace SecurityRule.Domain.Models;

public class FirewallRule
{
    public int Id { get; set; }

    // Source
    public int SourceServerId { get; set; }
    public Server? SourceServer { get; set; }
    public int SourceServiceId { get; set; }
    public AppService? SourceService { get; set; }

    // Destination
    public int DestinationServerId { get; set; }
    public Server? DestinationServer { get; set; }
    public int DestinationServiceId { get; set; }
    public AppService? DestinationService { get; set; }

    // Rule properties
    public string Protocol { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Direction { get; set; } = string.Empty;

    /// <summary>Null means the rule has no expiry (unlimited).</summary>
    public DateTime? ExpiresAt { get; set; }

    public string Description { get; set; } = string.Empty;
}
