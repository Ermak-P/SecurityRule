using Microsoft.EntityFrameworkCore;
using SecurityRule.Domain.Interfaces;
using SecurityRule.Domain.Models;
using SecurityRule.Infrastructure.Data;

namespace SecurityRule.Infrastructure.Repositories;

public class FirewallRuleRepository : IFirewallRuleRepository
{
    private readonly AppDbContext _context;

    public FirewallRuleRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<FirewallRule>> GetAllAsync()
        => await _context.FirewallRules.ToListAsync();

    public async Task<FirewallRule?> GetByIdAsync(int id)
        => await _context.FirewallRules.FindAsync(id);

    public async Task AddAsync(FirewallRule rule)
    {
        _context.FirewallRules.Add(rule);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(FirewallRule rule)
    {
        _context.FirewallRules.Update(rule);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var rule = await _context.FirewallRules.FindAsync(id);
        if (rule != null)
        {
            _context.FirewallRules.Remove(rule);
            await _context.SaveChangesAsync();
        }
    }
}
