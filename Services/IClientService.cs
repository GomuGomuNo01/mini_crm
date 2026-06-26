using MiniCrm.Models;

namespace MiniCrm.Services;

public interface IClientService
{
    Task<IEnumerable<Client>> GetAllAsync(string? search = null);
    Task<Client?> GetByIdAsync(int id);
    Task CreateAsync(Client client);
    // Met à jour le client et renvoie un résumé « avant → après » des changements.
    Task<string?> UpdateAsync(Client client);
    Task DeleteAsync(int id);
}
