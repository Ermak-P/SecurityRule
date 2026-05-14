namespace SecurityRule.Domain.Interfaces;

/// <summary>
/// Provides the current authenticated user name for auditing.
/// </summary>
public interface ICurrentUserAccessor
{
    string? GetCurrentUserName();
}
