using Microsoft.EntityFrameworkCore;
using SecurityRule.Domain.Interfaces;
using SecurityRule.Domain.Models;
using SecurityRule.Infrastructure.Data;

namespace SecurityRule.Infrastructure.Repositories;

public class AppServiceRepository : IAppServiceRepository
{
    private readonly AppDbContext _context;

    public AppServiceRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<AppService>> GetAllAsync()
        => await _context.AppServices
            .Include(s => s.Servers)
            .Include(s => s.Certificates)
            .ToListAsync();

    public async Task<AppService?> GetByIdAsync(int id)
        => await _context.AppServices
            .Include(s => s.Servers)
            .Include(s => s.Certificates)
            .FirstOrDefaultAsync(s => s.Id == id);

    public async Task AddAsync(AppService service)
    {
        _context.AppServices.Add(service);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(AppService service)
    {
        var existing = await _context.AppServices
            .Include(s => s.Servers)
            .FirstOrDefaultAsync(s => s.Id == service.Id);
        if (existing == null) return;

        existing.Name = service.Name;
        existing.AdAccountName = service.AdAccountName;

        existing.Servers.Clear();
        foreach (var server in service.Servers)
        {
            var trackedServer = await _context.Servers.FindAsync(server.Id);
            if (trackedServer != null)
                existing.Servers.Add(trackedServer);
        }

        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var service = await _context.AppServices.FindAsync(id);
        if (service != null)
        {
            _context.AppServices.Remove(service);
            await _context.SaveChangesAsync();
        }
    }
}
