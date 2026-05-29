using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using SecurityRule.Domain.Interfaces;
using SecurityRule.Domain.Models;

namespace SecurityRule.Infrastructure.Services;

/// <summary>
/// HTTP client implementation of <see cref="IPartnerService"/> that fetches
/// partner data from a configurable external REST API.
/// The base address is configured via the named HttpClient "PartnerService".
/// </summary>
public class PartnerService : IPartnerService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<PartnerService> _logger;

    public PartnerService(HttpClient httpClient, ILogger<PartnerService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<IEnumerable<PartnerInfo>> GetPartnersAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _httpClient.GetFromJsonAsync<IEnumerable<PartnerInfo>>(
                "partners", cancellationToken);
            return result ?? [];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch partners from external service");
            return [];
        }
    }
}
