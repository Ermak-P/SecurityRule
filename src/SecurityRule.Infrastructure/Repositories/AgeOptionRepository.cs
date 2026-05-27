using Microsoft.EntityFrameworkCore;
using SecurityRule.Domain.Interfaces;
using SecurityRule.Infrastructure.Data;

namespace SecurityRule.Infrastructure.Repositories;

public class AgeOptionRepository : IAgeOptionRepository
{
    private readonly AppDbContext _context;

    public AgeOptionRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<int>> GetAllValuesAsync(CancellationToken cancellationToken = default)
        => await _context.AgeOptions
            .AsNoTracking()
            .OrderBy(a => a.Value)
            .Select(a => a.Value)
            .ToListAsync(cancellationToken);
}
