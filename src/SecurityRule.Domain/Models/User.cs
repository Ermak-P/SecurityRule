namespace SecurityRule.Domain.Models;

public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ICollection<UserGroup> Groups { get; set; } = new List<UserGroup>();
    public ICollection<AppService> Services { get; set; } = new List<AppService>();
}
