using Microsoft.EntityFrameworkCore;
using SecurityRule.Domain.Interfaces;
using SecurityRule.Domain.Models;
using SecurityRule.Infrastructure.Data;

namespace SecurityRule.Infrastructure.Repositories;

public class PartnerNameRepository : IPartnerNameRepository
{
    private readonly AppDbContext _context;

    public PartnerNameRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<PartnerName>> GetAllAsync(CancellationToken cancellationToken = default)
        => await _context.PartnerNames
            .AsNoTracking()
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);

    public async Task<PartnerName> GetOrCreateAsync(string name, CancellationToken cancellationToken = default)
    {
        var partner = await _context.PartnerNames.FirstOrDefaultAsync(p => p.Name == name, cancellationToken);
        if (partner is not null) return partner;

        partner = new PartnerName { Name = name };
        _context.PartnerNames.Add(partner);
        await _context.SaveChangesAsync(cancellationToken);
        return partner;
    }
}
