using SecurityRule.Domain.Models;

namespace SecurityRule.Domain.Interfaces;

/// <summary>
/// Persistence layer for locally stored partner names.
/// </summary>
public interface IPartnerNameRepository
{
    Task<IEnumerable<PartnerName>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<PartnerName> GetOrCreateAsync(string name, CancellationToken cancellationToken = default);
}
