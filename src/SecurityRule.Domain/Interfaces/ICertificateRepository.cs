using SecurityRule.Domain.Models;

namespace SecurityRule.Domain.Interfaces;

public interface ICertificateRepository
{
    Task<IEnumerable<Certificate>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Certificate?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task AddAsync(Certificate certificate, CancellationToken cancellationToken = default);
    Task UpdateAsync(Certificate certificate, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
