using SecurityRule.Domain.Models;

namespace SecurityRule.Domain.Interfaces;

public interface IFirewallRuleRepository
{
    Task<IEnumerable<FirewallRule>> GetAllAsync();
    Task<FirewallRule?> GetByIdAsync(int id);
    Task AddAsync(FirewallRule rule);
    Task UpdateAsync(FirewallRule rule);
    Task DeleteAsync(int id);
}
