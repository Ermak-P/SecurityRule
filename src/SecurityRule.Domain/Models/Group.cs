namespace SecurityRule.Domain.Models;

public class Group
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ICollection<Group> ParentGroups { get; set; } = new List<Group>();
    public ICollection<Group> ChildGroups { get; set; } = new List<Group>();
    public ICollection<User> Users { get; set; } = new List<User>();
}
