using Microsoft.EntityFrameworkCore;
using SecurityRule.Domain.Interfaces;
using SecurityRule.Domain.Models;
using SecurityRule.Infrastructure.Data;

namespace SecurityRule.Infrastructure.Repositories;

public class ServerRepository : IServerRepository
{
    private readonly AppDbContext _context;

    public ServerRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Server>> GetAllAsync()
        => await _context.Servers
            .Include(s => s.Services)
                .ThenInclude(svc => svc.Servers)
            .Include(s => s.Services)
                .ThenInclude(svc => svc.Certificates)
            .Include(s => s.Tags)
            .ToListAsync();

    public async Task<Server?> GetByIdAsync(int id)
        => await _context.Servers
            .Include(s => s.Services)
                .ThenInclude(svc => svc.Servers)
            .Include(s => s.Services)
                .ThenInclude(svc => svc.Certificates)
            .Include(s => s.Tags)
            .Include(s => s.SourceConnections)
                .ThenInclude(r => r.SourceService)
            .Include(s => s.SourceConnections)
                .ThenInclude(r => r.DestinationServer)
            .Include(s => s.SourceConnections)
                .ThenInclude(r => r.DestinationService)
            .Include(s => s.DestinationConnections)
                .ThenInclude(r => r.SourceServer)
            .Include(s => s.DestinationConnections)
                .ThenInclude(r => r.SourceService)
            .Include(s => s.DestinationConnections)
                .ThenInclude(r => r.DestinationService)
            .FirstOrDefaultAsync(s => s.Id == id);

    public async Task AddAsync(Server server)
    {
        _context.Servers.Add(server);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Server server)
    {
        var existing = await _context.Servers
            .Include(s => s.Services)
            .Include(s => s.Tags)
            .FirstOrDefaultAsync(s => s.Id == server.Id);
        if (existing == null) return;

        existing.Name = server.Name;
        existing.IpAddress = server.IpAddress;
        existing.OperatingSystem = server.OperatingSystem;
        existing.Description = server.Description;

        var serviceIds = server.Services.Select(s => s.Id).ToList();
        var trackedServices = await _context.AppServices
            .Where(s => serviceIds.Contains(s.Id))
            .ToListAsync();

        existing.Services.Clear();
        foreach (var service in trackedServices)
            existing.Services.Add(service);

        var tagIds = server.Tags.Select(t => t.Id).ToList();
        var trackedTags = await _context.Tags
            .Where(t => tagIds.Contains(t.Id))
            .ToListAsync();

        existing.Tags.Clear();
        foreach (var tag in trackedTags)
            existing.Tags.Add(tag);

        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var server = await _context.Servers.FindAsync(id);
        if (server != null)
        {
            _context.Servers.Remove(server);
            await _context.SaveChangesAsync();
        }
    }
}
