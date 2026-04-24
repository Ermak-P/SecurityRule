namespace SecurityRule.Domain.Models;

public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int? CertificateId { get; set; }
    public Certificate? Certificate { get; set; }
    public ICollection<AppService> Services { get; set; } = new List<AppService>();
}
