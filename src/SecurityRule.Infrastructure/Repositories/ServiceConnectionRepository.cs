using Microsoft.EntityFrameworkCore;
using SecurityRule.Domain.Interfaces;
using SecurityRule.Domain.Models;
using SecurityRule.Infrastructure.Data;

namespace SecurityRule.Infrastructure.Repositories;

public class ServiceConnectionRepository : IServiceConnectionRepository
{
    private readonly AppDbContext _context;

    public ServiceConnectionRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<ServiceConnection>> GetAllAsync()
        => await _context.ServiceConnections
            .Include(c => c.SourceServer)
            .Include(c => c.SourceService)
            .Include(c => c.DestinationServer)
            .Include(c => c.DestinationService)
            .ToListAsync();

    public async Task<ServiceConnection?> GetByIdAsync(int id)
        => await _context.ServiceConnections
            .Include(c => c.SourceServer)
            .Include(c => c.SourceService)
            .Include(c => c.DestinationServer)
            .Include(c => c.DestinationService)
            .FirstOrDefaultAsync(c => c.Id == id);

    public async Task AddAsync(ServiceConnection connection)
    {
        _context.ServiceConnections.Add(connection);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(ServiceConnection connection)
    {
        var existing = await _context.ServiceConnections.FindAsync(connection.Id);
        if (existing == null) return;

        existing.SourceServerId      = connection.SourceServerId;
        existing.SourceServiceId     = connection.SourceServiceId;
        existing.DestinationServerId = connection.DestinationServerId;
        existing.DestinationServiceId = connection.DestinationServiceId;
        existing.Protocol = connection.Protocol;
        existing.Port     = connection.Port;

        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var connection = await _context.ServiceConnections.FindAsync(id);
        if (connection != null)
        {
            _context.ServiceConnections.Remove(connection);
            await _context.SaveChangesAsync();
        }
    }
}
