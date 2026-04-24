using SecurityRule.Domain.Models;

namespace SecurityRule.Domain.Interfaces;

public interface IServiceConnectionRepository
{
    Task<IEnumerable<ServiceConnection>> GetAllAsync();
    Task<ServiceConnection?> GetByIdAsync(int id);
    Task AddAsync(ServiceConnection connection);
    Task UpdateAsync(ServiceConnection connection);
    Task DeleteAsync(int id);
}
