using SecurityRule.Domain.Models;

namespace SecurityRule.Domain.Interfaces;

public interface IServerRepository
{
    Task<IEnumerable<Server>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Server?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task AddAsync(Server server, CancellationToken cancellationToken = default);
    Task UpdateAsync(Server server, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
