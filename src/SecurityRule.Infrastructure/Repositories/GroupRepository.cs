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

    public async Task<IEnumerable<Group>> GetAllAsync(CancellationToken cancellationToken = default)
        => await _context.Groups
            .AsNoTracking()
            .ToListAsync(cancellationToken);

    public async Task<Group?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => await _context.Groups
            .AsNoTracking()
            .FirstOrDefaultAsync(g => g.Id == id, cancellationToken);

    public async Task AddAsync(Group group, CancellationToken cancellationToken = default)
    {
        _context.Groups.Add(group);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Group group, CancellationToken cancellationToken = default)
    {
        var existing = await _context.Groups.FirstOrDefaultAsync(g => g.Id == group.Id, cancellationToken);
        if (existing == null) return;

        existing.Name = group.Name;
        existing.Description = group.Description;

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var group = await _context.Groups.FindAsync([id], cancellationToken);
        if (group != null)
        {
            _context.Groups.Remove(group);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
