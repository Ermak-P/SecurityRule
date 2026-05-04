namespace SecurityRule.Domain.Models;

public class Tag
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public ICollection<Server> Servers { get; set; } = new List<Server>();
    public ICollection<AppService> Services { get; set; } = new List<AppService>();
}
