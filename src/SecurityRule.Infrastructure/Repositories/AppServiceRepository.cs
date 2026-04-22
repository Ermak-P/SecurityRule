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
            .Include(s => s.User)
            .Include(s => s.Servers)
                .ThenInclude(srv => srv.Services)
            .Include(s => s.Certificates)
            .ToListAsync();

    public async Task<AppService?> GetByIdAsync(int id)
        => await _context.AppServices
            .Include(s => s.User)
            .Include(s => s.Servers)
                .ThenInclude(srv => srv.Services)
            .Include(s => s.Certificates)
            .Include(s => s.FirewallRules)
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
        existing.UserName = service.UserName;
        existing.Port = service.Port;

        var serverIds = service.Servers.Select(s => s.Id).ToList();
        var trackedServers = await _context.Servers
            .Where(s => serverIds.Contains(s.Id))
            .ToListAsync();

        existing.Servers.Clear();
        foreach (var server in trackedServers)
            existing.Servers.Add(server);

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
