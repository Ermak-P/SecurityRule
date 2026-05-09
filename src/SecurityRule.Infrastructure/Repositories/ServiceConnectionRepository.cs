using Microsoft.EntityFrameworkCore;
using SecurityRule.Domain.Interfaces;
using SecurityRule.Domain.Models;
using SecurityRule.Domain.Validation;
using SecurityRule.Infrastructure.Data;

namespace SecurityRule.Infrastructure.Repositories;

public class ServiceConnectionRepository : IServiceConnectionRepository
{
    private readonly AppDbContext _context;

    public ServiceConnectionRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<ServiceConnection>> GetAllAsync(CancellationToken cancellationToken = default)
        => await _context.ServiceConnections
            .AsNoTracking()
            .Include(c => c.SourceServer)
            .Include(c => c.SourceService)
            .Include(c => c.DestinationServer)
            .Include(c => c.DestinationService)
            .ToListAsync(cancellationToken);

    public async Task<ServiceConnection?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => await _context.ServiceConnections
            .AsNoTracking()
            .Include(c => c.SourceServer)
            .Include(c => c.SourceService)
            .Include(c => c.DestinationServer)
            .Include(c => c.DestinationService)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public async Task AddAsync(ServiceConnection connection, CancellationToken cancellationToken = default)
    {
        DomainInvariants.ValidateServiceConnection(connection);
        _context.ServiceConnections.Add(connection);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(ServiceConnection connection, CancellationToken cancellationToken = default)
    {
        DomainInvariants.ValidateServiceConnection(connection);

        var existing = await _context.ServiceConnections.FindAsync([connection.Id], cancellationToken);
        if (existing == null) return;

        existing.SourceServerId      = connection.SourceServerId;
        existing.SourceServiceId     = connection.SourceServiceId;
        existing.DestinationServerId = connection.DestinationServerId;
        existing.DestinationServiceId = connection.DestinationServiceId;
        existing.Protocol    = connection.Protocol;
        existing.Description = connection.Description;

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var connection = await _context.ServiceConnections.FindAsync([id], cancellationToken);
        if (connection != null)
        {
            _context.ServiceConnections.Remove(connection);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
