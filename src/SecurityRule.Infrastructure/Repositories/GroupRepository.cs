using Microsoft.EntityFrameworkCore;
using SecurityRule.Domain.Interfaces;
using SecurityRule.Domain.Models;
using SecurityRule.Infrastructure.Data;

namespace SecurityRule.Infrastructure.Repositories;

public class GroupRepository : IGroupRepository
{
    private readonly AppDbContext _context;

    public GroupRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Group>> GetAllAsync()
        => await _context.Groups.ToListAsync();

    public async Task<Group?> GetByIdAsync(int id)
        => await _context.Groups.FirstOrDefaultAsync(g => g.Id == id);

    public async Task AddAsync(Group group)
    {
        _context.Groups.Add(group);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Group group)
    {
        var existing = await _context.Groups.FirstOrDefaultAsync(g => g.Id == group.Id);
        if (existing == null) return;

        existing.Name = group.Name;
        existing.Description = group.Description;

        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var group = await _context.Groups.FindAsync(id);
        if (group != null)
        {
            _context.Groups.Remove(group);
            await _context.SaveChangesAsync();
        }
    }
}
