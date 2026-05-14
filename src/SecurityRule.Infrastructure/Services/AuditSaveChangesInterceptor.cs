using System.Security.Principal;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;

namespace SecurityRule.Infrastructure.Services;

/// <summary>
/// Centralized audit logging for all data mutations performed through AppDbContext.
/// </summary>
public sealed class AuditSaveChangesInterceptor : SaveChangesInterceptor
{
    private readonly ILogger<AuditSaveChangesInterceptor> _logger;

    public AuditSaveChangesInterceptor(ILogger<AuditSaveChangesInterceptor> logger)
    {
        _logger = logger;
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        LogAudit(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        LogAudit(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void LogAudit(DbContext? context)
    {
        if (context is null) return;

        var userName = Thread.CurrentPrincipal?.Identity?.Name;
        if (string.IsNullOrWhiteSpace(userName))
            userName = "anonymous";

        foreach (var entry in context.ChangeTracker.Entries()
                     .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted))
        {
            if (entry.Entity is null) continue;

            var entityName = entry.Metadata.ClrType.Name;
            var action = entry.State.ToString();
            var id = GetEntityId(entry);
            var changes = entry.State == EntityState.Modified
                ? string.Join(", ", entry.Properties
                    .Where(p => p.IsModified)
                    .Select(p => $"{p.Metadata.Name}:{FormatValue(p.OriginalValue)}->{FormatValue(p.CurrentValue)}"))
                : string.Empty;

            _logger.LogInformation(
                "AUDIT Action={Action} Entity={Entity} EntityId={EntityId} User={User} Changes={Changes}",
                action,
                entityName,
                id,
                userName,
                changes);
        }
    }

    private static object? GetEntityId(EntityEntry entry)
    {
        var keyProperty = entry.Metadata.FindPrimaryKey()?.Properties.FirstOrDefault();
        return keyProperty is null ? null : entry.Property(keyProperty.Name).CurrentValue;
    }

    private static string FormatValue(object? value) => value?.ToString() ?? "null";
}
