using Microsoft.EntityFrameworkCore;
using SecurityRule.Domain.Interfaces;
using SecurityRule.Domain.Models;
using SecurityRule.Infrastructure.Data;

namespace SecurityRule.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _context;

    public UserRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<User>> GetAllAsync()
        => await _context.Users
            .Include(u => u.Groups)
            .ToListAsync();

    public async Task<User?> GetByIdAsync(int id)
        => await _context.Users
            .Include(u => u.Groups)
            .Include(u => u.Services)
                .ThenInclude(s => s.Servers)
            .Include(u => u.Services)
                .ThenInclude(s => s.Certificates)
            .FirstOrDefaultAsync(u => u.Id == id);

    public async Task AddAsync(User user)
    {
        var groupIds = user.Groups.Select(g => g.Id).ToList();
        user.Groups.Clear();
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        if (groupIds.Count > 0)
        {
            var groups = await _context.Groups.Where(g => groupIds.Contains(g.Id)).ToListAsync();
            foreach (var group in groups)
                user.Groups.Add(group);
            await _context.SaveChangesAsync();
        }
    }

    public async Task UpdateAsync(User user)
    {
        var existing = await _context.Users
            .Include(u => u.Groups)
            .FirstOrDefaultAsync(u => u.Id == user.Id);
        if (existing == null) return;

        existing.Name = user.Name;
        existing.Description = user.Description;

        var groupIds = user.Groups.Select(g => g.Id).ToList();
        var groups = await _context.Groups.Where(g => groupIds.Contains(g.Id)).ToListAsync();

        existing.Groups.Clear();
        foreach (var group in groups)
            existing.Groups.Add(group);

        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user != null)
        {
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
        }
    }
}
