namespace SecurityRule.Domain.Models;

public class AppService
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int ServerId { get; set; }
    public string AdAccountName { get; set; } = string.Empty;
    public Server Server { get; set; } = null!;
    public ICollection<Certificate> Certificates { get; set; } = new List<Certificate>();
}
