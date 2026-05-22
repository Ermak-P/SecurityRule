using SecurityRule.Domain.Models;

namespace SecurityRule.Domain.Interfaces;

public interface IGroupRepository
{
    Task<IEnumerable<Group>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Group?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task AddAsync(Group group, CancellationToken cancellationToken = default);
    Task UpdateAsync(Group group, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
