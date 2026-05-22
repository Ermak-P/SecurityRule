using SecurityRule.Domain.Models;

namespace SecurityRule.Domain.Interfaces;

public interface ISearchService
{
    Task<IEnumerable<SearchResult>> SearchAsync(string query, CancellationToken cancellationToken = default);
}
