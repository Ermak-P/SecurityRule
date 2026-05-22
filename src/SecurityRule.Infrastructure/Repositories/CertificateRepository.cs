using Microsoft.EntityFrameworkCore;
using SecurityRule.Domain.Interfaces;
using SecurityRule.Domain.Models;
using SecurityRule.Infrastructure.Data;

namespace SecurityRule.Infrastructure.Repositories;

public class CertificateRepository : ICertificateRepository
{
    private readonly AppDbContext _context;

    public CertificateRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Certificate>> GetAllAsync(CancellationToken cancellationToken = default)
        => await _context.Certificates
            .AsNoTracking()
            .Include(c => c.Services)
            .ToListAsync(cancellationToken);

    public async Task<Certificate?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => await _context.Certificates
            .AsNoTracking()
            .Include(c => c.Services)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public async Task AddAsync(Certificate certificate, CancellationToken cancellationToken = default)
    {
        _context.Certificates.Add(certificate);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Certificate certificate, CancellationToken cancellationToken = default)
    {
        _context.Certificates.Update(certificate);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var cert = await _context.Certificates.FindAsync([id], cancellationToken);
        if (cert != null)
        {
            _context.Certificates.Remove(cert);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
