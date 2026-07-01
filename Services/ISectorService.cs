using MiniCrm.Models;
using MiniCrm.ViewModels;

namespace MiniCrm.Services;

public interface ISectorService
{
    Task<IReadOnlyList<Sector>> GetAllAsync();
    Task<PagedResult<Sector>> GetPagedAsync(string? search = null, int page = 1, int pageSize = 8);
    Task<IReadOnlyList<string>> GetNamesAsync();
    Task<Sector?> GetByIdAsync(int id);
    Task<int> GetClientCountAsync(string sectorName);

    Task<(bool Ok, string? Error)> CreateAsync(string name);
    Task<(bool Ok, string? Error, string? OldName)> UpdateAsync(int id, string name);
    Task<string?> DeleteAsync(int id);
}
