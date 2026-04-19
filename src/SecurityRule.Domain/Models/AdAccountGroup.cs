namespace SecurityRule.Domain.Models;

public class AdAccountGroup
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int AdAccountId { get; set; }
    public AdAccount AdAccount { get; set; } = null!;
}
