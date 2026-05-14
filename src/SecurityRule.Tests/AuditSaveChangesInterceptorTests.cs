using System.Security.Claims;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SecurityRule.Domain.Models;
using SecurityRule.Infrastructure.Data;
using SecurityRule.Infrastructure.Services;

namespace SecurityRule.Tests;

[TestFixture]
public class AuditSaveChangesInterceptorTests
{
    private AppDbContext _context = null!;
    private ListLogger<AuditSaveChangesInterceptor> _logger = null!;

    [SetUp]
    public void SetUp()
    {
        _logger = new ListLogger<AuditSaveChangesInterceptor>();
        var interceptor = new AuditSaveChangesInterceptor(_logger);

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .AddInterceptors(interceptor)
            .Options;

        _context = new AppDbContext(options);
    }

    [TearDown]
    public void TearDown()
    {
        _context.Dispose();
    }

    [Test]
    public async Task SaveChanges_ShouldWriteAuditLog_ForAddedEntity()
    {
        Thread.CurrentPrincipal = new ClaimsPrincipal(
            new ClaimsIdentity([new Claim(ClaimTypes.Name, "audit.user")], "Test"));

        _context.Servers.Add(new Server
        {
            Name = "Srv-Audit",
            IpAddress = "10.0.0.1",
            OperatingSystem = "Linux"
        });

        await _context.SaveChangesAsync();

        _logger.Messages.Should().Contain(m =>
            m.Contains("AUDIT Action=Added") &&
            m.Contains("Entity=Server") &&
            m.Contains("User=audit.user"));
    }

    [Test]
    public async Task SaveChanges_ShouldWriteAuditLog_ForModifiedEntity()
    {
        var server = new Server
        {
            Name = "Srv-1",
            IpAddress = "10.0.0.2",
            OperatingSystem = "Linux"
        };
        _context.Servers.Add(server);
        await _context.SaveChangesAsync();
        _logger.Messages.Clear();

        server.Name = "Srv-2";
        await _context.SaveChangesAsync();

        _logger.Messages.Should().Contain(m =>
            m.Contains("AUDIT Action=Modified") &&
            m.Contains("Entity=Server") &&
            m.Contains("Name:Srv-1->Srv-2"));
    }

    private sealed class ListLogger<T> : ILogger<T>
    {
        public List<string> Messages { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Messages.Add(formatter(state, exception));
        }
    }
}
