using SecurityRule.Domain.Models;

namespace SecurityRule.Domain.Interfaces;

public interface IServiceConnectionRepository
{
    Task<IEnumerable<ServiceConnection>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<ServiceConnection?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task AddAsync(ServiceConnection connection, CancellationToken cancellationToken = default);
    Task UpdateAsync(ServiceConnection connection, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
