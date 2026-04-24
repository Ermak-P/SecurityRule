namespace SecurityRule.Domain.Models;

public class Certificate
{
    public int Id { get; set; }
    public string SerialNumber { get; set; } = string.Empty;
    public string Thumbprint { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string RequestNumber { get; set; } = string.Empty;
    public DateTime IssuedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public ICollection<AppService> Services { get; set; } = new List<AppService>();
    public ICollection<User> Users { get; set; } = new List<User>();
}
