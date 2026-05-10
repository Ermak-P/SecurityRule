using Microsoft.EntityFrameworkCore;
using SecurityRule.Domain.Interfaces;
using SecurityRule.Domain.Models;
using SecurityRule.Domain.Validation;
using SecurityRule.Infrastructure.Data;

namespace SecurityRule.Infrastructure.Repositories;

public class ServerRepository : IServerRepository
{
    private readonly AppDbContext _context;

    public ServerRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Server>> GetAllAsync(CancellationToken cancellationToken = default)
        => await _context.Servers
            .AsNoTrackingWithIdentityResolution()
            .Include(s => s.Services)
            .Include(s => s.Services)
                .ThenInclude(svc => svc.Certificates)
            .Include(s => s.Tags)
            .ToListAsync(cancellationToken);

    public async Task<Server?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => await _context.Servers
            .AsNoTrackingWithIdentityResolution()
            .Include(s => s.Services)
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
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    public async Task AddAsync(Server server, CancellationToken cancellationToken = default)
    {
        DomainInvariants.ValidateServer(server);

        var serviceIds = server.Services.Select(s => s.Id).Distinct().ToList();
        var trackedServices = await _context.AppServices
            .Where(s => serviceIds.Contains(s.Id))
            .ToListAsync(cancellationToken);

        var tagIds = server.Tags.Select(t => t.Id).Distinct().ToList();
        var trackedTags = await _context.Tags
            .Where(t => tagIds.Contains(t.Id))
            .ToListAsync(cancellationToken);

        server.Services = trackedServices;
        server.Tags = trackedTags;

        _context.Servers.Add(server);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Server server, CancellationToken cancellationToken = default)
    {
        DomainInvariants.ValidateServer(server);

        var existing = await _context.Servers
            .Include(s => s.Services)
            .Include(s => s.Tags)
            .FirstOrDefaultAsync(s => s.Id == server.Id, cancellationToken);
        if (existing == null) return;

        existing.Name = server.Name;
        existing.IpAddress = server.IpAddress;
        existing.OperatingSystem = server.OperatingSystem;
        existing.Description = server.Description;

        var serviceIds = server.Services.Select(s => s.Id).ToList();
        var trackedServices = await _context.AppServices
            .Where(s => serviceIds.Contains(s.Id))
            .ToListAsync(cancellationToken);

        existing.Services.Clear();
        foreach (var service in trackedServices)
            existing.Services.Add(service);

        var tagIds = server.Tags.Select(t => t.Id).ToList();
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
        var server = await _context.Servers.FindAsync([id], cancellationToken);
        if (server != null)
        {
            _context.Servers.Remove(server);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
