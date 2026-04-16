using Microsoft.EntityFrameworkCore;
using SecurityRule.Infrastructure.Data;
using SecurityRule.Infrastructure.Repositories;

namespace SecurityRule.BDD.Tests.Support;

/// <summary>
/// Shared test state for a single scenario.
/// Registered as a per-scenario singleton in Reqnroll's DI container
/// so that multiple step definition classes can share the same in-memory
/// database and communicate context (last-created IDs, found entities, etc.).
/// </summary>
public class ScenarioState : IDisposable
{
    public AppDbContext DbContext { get; }
    public ServerRepository ServerRepository { get; }
    public AppServiceRepository AppServiceRepository { get; }
    public CertificateRepository CertificateRepository { get; }
    public FirewallRuleRepository FirewallRuleRepository { get; }

    // ── shared state across step definition classes ──────────────────────────
    public int LastServerId { get; set; }
    public int LastServiceId { get; set; }
    public int LastCertificateId { get; set; }
    public int LastFirewallRuleId { get; set; }
    public Exception? ThrownException { get; set; }

    public ScenarioState()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        DbContext = new AppDbContext(options);
        ServerRepository = new ServerRepository(DbContext);
        AppServiceRepository = new AppServiceRepository(DbContext);
        CertificateRepository = new CertificateRepository(DbContext);
        FirewallRuleRepository = new FirewallRuleRepository(DbContext);
    }

    public void Dispose() => DbContext.Dispose();
}
