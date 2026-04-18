using SecurityRule.Domain.Models;

namespace SecurityRule.Domain.Interfaces;

public interface IOperatingSystemRepository
{
    Task<IEnumerable<OperatingSystemOption>> GetAllAsync();
}
