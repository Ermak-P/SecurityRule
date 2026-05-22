namespace SecurityRule.Domain.Models;

public class AppService
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public int? UserId { get; set; }
    public int? Port { get; set; }
    public User? User { get; set; }
    public ICollection<Server> Servers { get; set; } = new List<Server>();
    public ICollection<Certificate> Certificates { get; set; } = new List<Certificate>();
    public ICollection<Tag> Tags { get; set; } = new List<Tag>();
    public ICollection<ServiceConnection> SourceConnections { get; set; } = new List<ServiceConnection>();
    public ICollection<ServiceConnection> DestinationConnections { get; set; } = new List<ServiceConnection>();
    public ICollection<PartnerName> Partners { get; set; } = new List<PartnerName>();
}
