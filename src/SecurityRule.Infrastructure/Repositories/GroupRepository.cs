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
        => await _context.Groups
            .Include(g => g.Users)
            .Include(g => g.ChildGroups)
            .ToListAsync();

    public async Task<Group?> GetByIdAsync(int id)
        => await _context.Groups
            .Include(g => g.Users)
            .Include(g => g.ChildGroups)
            .Include(g => g.ParentGroups)
            .FirstOrDefaultAsync(g => g.Id == id);

    public async Task AddAsync(Group group)
    {
        var childIds = group.ChildGroups.Select(c => c.Id).ToList();
        group.ChildGroups.Clear();
        _context.Groups.Add(group);
        await _context.SaveChangesAsync();

        if (childIds.Count > 0)
        {
            var children = await _context.Groups.Where(g => childIds.Contains(g.Id)).ToListAsync();
            foreach (var child in children)
                group.ChildGroups.Add(child);
            await _context.SaveChangesAsync();
        }
    }

    public async Task UpdateAsync(Group group)
    {
        var existing = await _context.Groups
            .Include(g => g.ChildGroups)
            .Include(g => g.ParentGroups)
            .FirstOrDefaultAsync(g => g.Id == group.Id);
        if (existing == null) return;

        existing.Name = group.Name;
        existing.Description = group.Description;

        var childIds = group.ChildGroups.Select(c => c.Id).ToList();
        var children = await _context.Groups.Where(g => childIds.Contains(g.Id)).ToListAsync();

        existing.ChildGroups.Clear();
        foreach (var child in children)
            existing.ChildGroups.Add(child);

        var parentIds = group.ParentGroups.Select(p => p.Id).ToList();
        var parents = await _context.Groups.Where(g => parentIds.Contains(g.Id)).ToListAsync();

        existing.ParentGroups.Clear();
        foreach (var parent in parents)
            existing.ParentGroups.Add(parent);

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
