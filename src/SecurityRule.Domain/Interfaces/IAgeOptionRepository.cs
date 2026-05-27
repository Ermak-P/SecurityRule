using SecurityRule.Domain.Models;

namespace SecurityRule.Domain.Interfaces;

/// <summary>
/// Provides read access to the age reference dictionary.
/// </summary>
public interface IAgeOptionRepository
{
    Task<IReadOnlyList<int>> GetAllValuesAsync(CancellationToken cancellationToken = default);
}
