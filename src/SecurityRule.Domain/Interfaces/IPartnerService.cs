using SecurityRule.Domain.Models;

namespace SecurityRule.Domain.Interfaces;

/// <summary>
/// Provides access to the external partner catalogue.
/// </summary>
public interface IPartnerService
{
    Task<IEnumerable<PartnerInfo>> GetPartnersAsync(CancellationToken cancellationToken = default);
}
