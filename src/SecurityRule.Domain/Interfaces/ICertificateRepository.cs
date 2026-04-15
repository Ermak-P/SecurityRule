using SecurityRule.Domain.Models;

namespace SecurityRule.Domain.Interfaces;

public interface ICertificateRepository
{
    Task<IEnumerable<Certificate>> GetAllAsync();
    Task<Certificate?> GetByIdAsync(int id);
    Task AddAsync(Certificate certificate);
    Task UpdateAsync(Certificate certificate);
    Task DeleteAsync(int id);
}
