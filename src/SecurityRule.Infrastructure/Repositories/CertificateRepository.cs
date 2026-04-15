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

    public async Task<IEnumerable<Certificate>> GetAllAsync()
        => await _context.Certificates.Include(c => c.Services).ToListAsync();

    public async Task<Certificate?> GetByIdAsync(int id)
        => await _context.Certificates.Include(c => c.Services).FirstOrDefaultAsync(c => c.Id == id);

    public async Task AddAsync(Certificate certificate)
    {
        _context.Certificates.Add(certificate);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Certificate certificate)
    {
        _context.Certificates.Update(certificate);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var cert = await _context.Certificates.FindAsync(id);
        if (cert != null)
        {
            _context.Certificates.Remove(cert);
            await _context.SaveChangesAsync();
        }
    }
}
