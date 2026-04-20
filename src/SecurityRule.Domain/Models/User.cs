namespace SecurityRule.Domain.Models;

public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ICollection<Group> Groups { get; set; } = new List<Group>();
    public ICollection<AppService> Services { get; set; } = new List<AppService>();
}
