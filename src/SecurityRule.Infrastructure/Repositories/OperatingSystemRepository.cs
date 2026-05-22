using Microsoft.EntityFrameworkCore;
using SecurityRule.Domain.Interfaces;
using SecurityRule.Domain.Models;
using SecurityRule.Infrastructure.Data;

namespace SecurityRule.Infrastructure.Repositories;

public class OperatingSystemRepository : IOperatingSystemRepository
{
    private readonly AppDbContext _context;

    public OperatingSystemRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<OperatingSystemOption>> GetAllAsync(CancellationToken cancellationToken = default)
        => await _context.OperatingSystemOptions
            .AsNoTracking()
            .OrderBy(o => o.Name)
            .ToListAsync(cancellationToken);
}
