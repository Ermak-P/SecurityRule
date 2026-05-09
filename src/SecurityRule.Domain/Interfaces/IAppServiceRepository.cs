using SecurityRule.Domain.Models;

namespace SecurityRule.Domain.Interfaces;

public interface IAppServiceRepository
{
    Task<IEnumerable<AppService>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<AppService?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task AddAsync(AppService service, CancellationToken cancellationToken = default);
    Task UpdateAsync(AppService service, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
