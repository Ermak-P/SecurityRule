using SecurityRule.Domain.Models;

namespace SecurityRule.Domain.Interfaces;

public interface IAppServiceRepository
{
    Task<IEnumerable<AppService>> GetAllAsync();
    Task<AppService?> GetByIdAsync(int id);
    Task AddAsync(AppService service);
    Task UpdateAsync(AppService service);
    Task DeleteAsync(int id);
}
