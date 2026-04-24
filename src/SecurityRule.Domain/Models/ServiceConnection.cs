namespace SecurityRule.Domain.Models;

public class ServiceConnection
{
    public int Id { get; set; }

    // Source (both optional; for source server, service may be omitted)
    public int? SourceServerId { get; set; }
    public Server? SourceServer { get; set; }

    public int? SourceServiceId { get; set; }
    public AppService? SourceService { get; set; }

    // Destination (service is required, server is optional)
    public int? DestinationServerId { get; set; }
    public Server? DestinationServer { get; set; }

    public int DestinationServiceId { get; set; }
    public AppService? DestinationService { get; set; }

    // Connection properties
    public string Protocol { get; set; } = string.Empty;
    public int? Port { get; set; }
}
