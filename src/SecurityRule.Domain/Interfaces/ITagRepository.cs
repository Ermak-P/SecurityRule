using SecurityRule.Domain.Models;

namespace SecurityRule.Domain.Interfaces;

public interface ITagRepository
{
    Task<IEnumerable<Tag>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Tag> GetOrCreateAsync(string name, CancellationToken cancellationToken = default);
}
