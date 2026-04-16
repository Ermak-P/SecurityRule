namespace SecurityRule.Domain.Models;

public class Certificate
{
    public int Id { get; set; }
    public DateTime IssuedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public string Description { get; set; } = string.Empty;
    public ICollection<AppService> Services { get; set; } = new List<AppService>();
}
