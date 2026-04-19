namespace SecurityRule.Domain.Models;

public class AdAccount
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ICollection<AdAccountGroup> Groups { get; set; } = new List<AdAccountGroup>();
    public ICollection<AppService> Services { get; set; } = new List<AppService>();
}
