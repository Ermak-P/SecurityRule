using SecurityRule.Domain.Models;

namespace SecurityRule.Domain.Interfaces;

public interface IGroupRepository
{
    Task<IEnumerable<Group>> GetAllAsync();
    Task<Group?> GetByIdAsync(int id);
    Task AddAsync(Group group);
    Task UpdateAsync(Group group);
    Task DeleteAsync(int id);
}
