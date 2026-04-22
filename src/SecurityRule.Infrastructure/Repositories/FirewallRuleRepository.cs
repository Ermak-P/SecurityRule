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
        => await _context.FirewallRules
            .Include(r => r.Server)
            .Include(r => r.Service)
            .ToListAsync();

    public async Task<FirewallRule?> GetByIdAsync(int id)
        => await _context.FirewallRules
            .Include(r => r.Server)
            .Include(r => r.Service)
            .FirstOrDefaultAsync(r => r.Id == id);

    public async Task AddAsync(FirewallRule rule)
    {
        _context.FirewallRules.Add(rule);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(FirewallRule rule)
    {
        var existing = await _context.FirewallRules.FindAsync(rule.Id);
        if (existing == null) return;

        existing.SourceIp = rule.SourceIp;
        existing.DestinationIp = rule.DestinationIp;
        existing.DestinationPort = rule.DestinationPort;
        existing.Protocol = rule.Protocol;
        existing.Action = rule.Action;
        existing.Direction = rule.Direction;
        existing.ExpiresAt = rule.ExpiresAt;
        existing.Description = rule.Description;
        existing.ServerId = rule.ServerId;
        existing.ServiceId = rule.ServiceId;

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
