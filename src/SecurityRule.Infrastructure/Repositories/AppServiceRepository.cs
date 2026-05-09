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

    public async Task<IEnumerable<AppService>> GetAllAsync(CancellationToken cancellationToken = default)
        => await _context.AppServices
            .AsNoTracking()
            .Include(s => s.User)
            .Include(s => s.Servers)
                .ThenInclude(srv => srv.Services)
            .Include(s => s.Certificates)
            .Include(s => s.Tags)
            .ToListAsync(cancellationToken);

    public async Task<AppService?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => await _context.AppServices
            .AsNoTracking()
            .Include(s => s.User)
            .Include(s => s.Servers)
                .ThenInclude(srv => srv.Services)
            .Include(s => s.Certificates)
            .Include(s => s.Tags)
            .Include(s => s.SourceConnections)
                .ThenInclude(r => r.SourceServer)
            .Include(s => s.SourceConnections)
                .ThenInclude(r => r.DestinationServer)
            .Include(s => s.SourceConnections)
                .ThenInclude(r => r.DestinationService)
            .Include(s => s.DestinationConnections)
                .ThenInclude(r => r.SourceServer)
            .Include(s => s.DestinationConnections)
                .ThenInclude(r => r.SourceService)
            .Include(s => s.DestinationConnections)
                .ThenInclude(r => r.DestinationServer)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    public async Task AddAsync(AppService service, CancellationToken cancellationToken = default)
    {
        _context.AppServices.Add(service);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(AppService service, CancellationToken cancellationToken = default)
    {
        var existing = await _context.AppServices
            .Include(s => s.Servers)
            .Include(s => s.Tags)
            .FirstOrDefaultAsync(s => s.Id == service.Id, cancellationToken);
        if (existing == null) return;

        existing.Name = service.Name;
        existing.UserName = service.UserName;
        existing.Port = service.Port;

        var serverIds = service.Servers.Select(s => s.Id).ToList();
        var trackedServers = await _context.Servers
            .Where(s => serverIds.Contains(s.Id))
            .ToListAsync(cancellationToken);

        existing.Servers.Clear();
        foreach (var server in trackedServers)
            existing.Servers.Add(server);

        var tagIds = service.Tags.Select(t => t.Id).ToList();
        var trackedTags = await _context.Tags
            .Where(t => tagIds.Contains(t.Id))
            .ToListAsync(cancellationToken);

        existing.Tags.Clear();
        foreach (var tag in trackedTags)
            existing.Tags.Add(tag);

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var service = await _context.AppServices.FindAsync([id], cancellationToken);
        if (service != null)
        {
            _context.AppServices.Remove(service);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
