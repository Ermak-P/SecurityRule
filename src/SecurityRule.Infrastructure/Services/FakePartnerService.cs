using SecurityRule.Domain.Interfaces;
using SecurityRule.Domain.Models;

namespace SecurityRule.Infrastructure.Services;

/// <summary>
/// In-memory stub of <see cref="IPartnerService"/> used during development and testing
/// when the real external partner service is not available.
/// Call <see cref="SetPartners"/> to pre-populate the partner list before each test scenario.
/// </summary>
public class FakePartnerService : IPartnerService
{
    private List<PartnerInfo> _partners = [];

    /// <summary>Replaces the current partner list with the supplied collection.</summary>
    public void SetPartners(IEnumerable<PartnerInfo> partners)
        => _partners = partners.ToList();

    /// <summary>Resets the partner list to empty.</summary>
    public void Reset() => _partners = [];

    public Task<IEnumerable<PartnerInfo>> GetPartnersAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IEnumerable<PartnerInfo>>(_partners);
}
