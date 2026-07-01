using MiniCrm.Models;
using MiniCrm.ViewModels;

namespace MiniCrm.Services;

public interface IClientService
{
    Task<IEnumerable<Client>> GetAllAsync(string? search = null);
    Task<PagedResult<Client>> GetPagedAsync(string? search = null, int page = 1, int pageSize = 8);
    Task<IReadOnlyList<string>> GetSectorsAsync();
    Task<Client?> GetByIdAsync(int id);
    Task CreateAsync(Client client);
    Task UpdateAsync(Client client);
    Task DeleteAsync(int id);
}
