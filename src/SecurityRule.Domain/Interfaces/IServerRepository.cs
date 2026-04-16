using SecurityRule.Domain.Models;

namespace SecurityRule.Domain.Interfaces;

public interface IServerRepository
{
    Task<IEnumerable<Server>> GetAllAsync();
    Task<Server?> GetByIdAsync(int id);
    Task AddAsync(Server server);
    Task UpdateAsync(Server server);
    Task DeleteAsync(int id);
}
