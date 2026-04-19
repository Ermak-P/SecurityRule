using Microsoft.EntityFrameworkCore;
using SecurityRule.Domain.Interfaces;
using SecurityRule.Domain.Models;
using SecurityRule.Infrastructure.Data;

namespace SecurityRule.Infrastructure.Repositories;

public class AdAccountRepository : IAdAccountRepository
{
    private readonly AppDbContext _context;

    public AdAccountRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<AdAccount>> GetAllAsync()
        => await _context.AdAccounts
            .Include(a => a.Groups)
            .ToListAsync();

    public async Task<AdAccount?> GetByIdAsync(int id)
        => await _context.AdAccounts
            .Include(a => a.Groups)
            .Include(a => a.Services)
                .ThenInclude(s => s.Servers)
            .Include(a => a.Services)
                .ThenInclude(s => s.Certificates)
            .FirstOrDefaultAsync(a => a.Id == id);

    public async Task AddAsync(AdAccount account)
    {
        _context.AdAccounts.Add(account);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(AdAccount account)
    {
        var existing = await _context.AdAccounts
            .Include(a => a.Groups)
            .FirstOrDefaultAsync(a => a.Id == account.Id);
        if (existing == null) return;

        existing.Name = account.Name;
        existing.Description = account.Description;

        existing.Groups.Clear();
        foreach (var group in account.Groups)
            existing.Groups.Add(new AdAccountGroup { Name = group.Name, AdAccountId = existing.Id });

        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var account = await _context.AdAccounts.FindAsync(id);
        if (account != null)
        {
            _context.AdAccounts.Remove(account);
            await _context.SaveChangesAsync();
        }
    }
}
