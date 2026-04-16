namespace SecurityRule.Domain.Models;

public class AppService
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string AdAccountName { get; set; } = string.Empty;
    public ICollection<Server> Servers { get; set; } = new List<Server>();
    public ICollection<Certificate> Certificates { get; set; } = new List<Certificate>();
}
