using MiniCrm.Models;
using MiniCrm.ViewModels;

namespace MiniCrm.Services;

public interface IContractService
{
    Task<PagedResult<Contract>> GetPagedAsync(
        string? search = null,
        ContractStatus? status = null,
        int page = 1,
        int pageSize = 10);

    Task<IEnumerable<Contract>> GetFilteredAsync(
        string? search = null,
        ContractStatus? status = null);

    Task<Contract?> GetByIdAsync(int id);
    Task CreateAsync(Contract contract);
    Task UpdateAsync(Contract contract);
    Task DeleteAsync(int id);
}
