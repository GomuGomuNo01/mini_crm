using Microsoft.EntityFrameworkCore;
using MiniCrm.Data;
using MiniCrm.Models;
using MiniCrm.ViewModels;

namespace MiniCrm.Services;

public class ContractService : IContractService
{
    private readonly ApplicationDbContext _context;

    public ContractService(ApplicationDbContext context)
    {
        _context = context;
    }

    private IQueryable<Contract> BuildQuery(string? search, ContractStatus? status)
    {
        var query = _context.Contracts
            .Include(c => c.Client)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();
            query = query.Where(c =>
                c.Title.Contains(search) ||
                c.Client.Name.Contains(search));
        }

        if (status.HasValue)
        {
            query = query.Where(c => c.Status == status.Value);
        }

        return query.OrderByDescending(c => c.StartDate);
    }

    public async Task<PagedResult<Contract>> GetPagedAsync(
        string? search = null,
        ContractStatus? status = null,
        int page = 1,
        int pageSize = 10)
    {
        if (page < 1) page = 1;
        var query = BuildQuery(search, status);

        var total = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<Contract>
        {
            Items = items,
            PageNumber = page,
            PageSize = pageSize,
            TotalCount = total
        };
    }

    public async Task<IEnumerable<Contract>> GetFilteredAsync(
        string? search = null,
        ContractStatus? status = null)
    {
        return await BuildQuery(search, status).ToListAsync();
    }

    public async Task<Contract?> GetByIdAsync(int id)
    {
        return await _context.Contracts
            .Include(c => c.Client)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task CreateAsync(Contract contract)
    {
        _context.Contracts.Add(contract);
        await _context.SaveChangesAsync();
    }

    public async Task<string?> UpdateAsync(Contract contract)
    {
        var existing = await _context.Contracts
            .Include(c => c.Client)
            .FirstOrDefaultAsync(c => c.Id == contract.Id);
        if (existing == null) return null;

        var changes = new List<string>();
        if (existing.Title != contract.Title)
            changes.Add($"Titre : « {existing.Title} » → « {contract.Title} »");
        if (existing.Amount != contract.Amount)
            changes.Add($"Montant : « {existing.Amount:N2} € » → « {contract.Amount:N2} € »");
        if (existing.StartDate.Date != contract.StartDate.Date)
            changes.Add($"Début : « {existing.StartDate:dd/MM/yyyy} » → « {contract.StartDate:dd/MM/yyyy} »");
        if (existing.EndDate.Date != contract.EndDate.Date)
            changes.Add($"Fin : « {existing.EndDate:dd/MM/yyyy} » → « {contract.EndDate:dd/MM/yyyy} »");
        if (existing.Status != contract.Status)
            changes.Add($"Statut : « {existing.Status} » → « {contract.Status} »");
        if (existing.ClientId != contract.ClientId)
        {
            var newClientName = await _context.Clients
                .Where(c => c.Id == contract.ClientId)
                .Select(c => c.Name)
                .FirstOrDefaultAsync();
            changes.Add($"Client : « {existing.Client?.Name ?? "—"} » → « {newClientName ?? "—"} »");
        }

        existing.Title = contract.Title;
        existing.Amount = contract.Amount;
        existing.StartDate = contract.StartDate;
        existing.EndDate = contract.EndDate;
        existing.Status = contract.Status;
        existing.ClientId = contract.ClientId;

        await _context.SaveChangesAsync();

        return changes.Count > 0 ? string.Join(" ; ", changes) : "Aucun changement";
    }

    public async Task DeleteAsync(int id)
    {
        var contract = await _context.Contracts.FindAsync(id);
        if (contract != null)
        {
            _context.Contracts.Remove(contract);
            await _context.SaveChangesAsync();
        }
    }
}
