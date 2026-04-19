using SecurityRule.Domain.Models;

namespace SecurityRule.Domain.Interfaces;

public interface IAdAccountRepository
{
    Task<IEnumerable<AdAccount>> GetAllAsync();
    Task<AdAccount?> GetByIdAsync(int id);
    Task AddAsync(AdAccount account);
    Task UpdateAsync(AdAccount account);
    Task DeleteAsync(int id);
}
