using SecurityRule.Domain.Models;

namespace SecurityRule.Domain.Interfaces;

public interface ITagRepository
{
    Task<IEnumerable<Tag>> GetAllAsync();
    Task<Tag> GetOrCreateAsync(string name);
}
