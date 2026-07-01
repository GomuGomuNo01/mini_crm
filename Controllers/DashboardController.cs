using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniCrm.Data;
using MiniCrm.Models;
using MiniCrm.ViewModels;

namespace MiniCrm.Controllers;

[Authorize]
public class DashboardController : Controller
{
    private readonly ApplicationDbContext _context;

    public DashboardController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: /Dashboard
    public async Task<IActionResult> Index()
    {
        var now = DateTime.UtcNow;
        var soon = now.AddDays(30);

        var contracts = await _context.Contracts
            .Include(c => c.Client)
            .ToListAsync();

        var activeContracts = contracts.Where(c => c.Status == ContractStatus.Active).ToList();

        var expiring = activeContracts
            .Where(c => c.EndDate >= now && c.EndDate <= soon)
            .OrderBy(c => c.EndDate)
            .ToList();

        var vm = new DashboardViewModel
        {
            TotalActiveClients = await _context.Clients.CountAsync(c => c.Status == ClientStatus.Active),
            TotalActiveContracts = activeContracts.Count,
            ContractsExpiringSoon = expiring.Count,
            TotalContractValue = activeContracts.Sum(c => c.Amount),
            ExpiringContracts = expiring.Take(5).ToList(),
            ContractsByStatus = Enum.GetValues<ContractStatus>()
                .ToDictionary(
                    s => s.ToString(),
                    s => contracts.Count(c => c.Status == s))
        };

        return View(vm);
    }
}
