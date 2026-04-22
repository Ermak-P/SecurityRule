namespace SecurityRule.Domain.Models;

public class Server
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public string OperatingSystem { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ICollection<AppService> Services { get; set; } = new List<AppService>();
    public ICollection<FirewallRule> SourceFirewallRules { get; set; } = new List<FirewallRule>();
    public ICollection<FirewallRule> DestinationFirewallRules { get; set; } = new List<FirewallRule>();
}
