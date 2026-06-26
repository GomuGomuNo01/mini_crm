using MiniCrm.Models;

namespace MiniCrm.Services;

public interface IClientService
{
    Task<IEnumerable<Client>> GetAllAsync(string? search = null);
    Task<Client?> GetByIdAsync(int id);
    Task CreateAsync(Client client);
    Task UpdateAsync(Client client);
    Task DeleteAsync(int id);
}
