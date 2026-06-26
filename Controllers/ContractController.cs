using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using MiniCrm.Models;
using MiniCrm.Services;

namespace MiniCrm.Controllers;

[Authorize]
public class ContractController : Controller
{
    private readonly IContractService _contractService;
    private readonly IClientService _clientService;
    private readonly IAuditService _auditService;

    public ContractController(IContractService contractService, IClientService clientService, IAuditService auditService)
    {
        _contractService = contractService;
        _clientService = clientService;
        _auditService = auditService;
    }

    // GET: /Contract
    public async Task<IActionResult> Index(string? search, ContractStatus? status, int page = 1)
    {
        var result = await _contractService.GetPagedAsync(search, status, page, pageSize: 10);

        ViewBag.Search = search;
        ViewBag.Status = status;
        return View(result);
    }

    // GET: /Contract/Details/5
    public async Task<IActionResult> Details(int id)
    {
        var contract = await _contractService.GetByIdAsync(id);
        if (contract == null) return NotFound();
        return View(contract);
    }

    // GET: /Contract/Create
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create()
    {
        await PopulateClientsAsync();
        return View(new Contract());
    }

    // POST: /Contract/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create(Contract contract)
    {
        if (!ModelState.IsValid)
        {
            await PopulateClientsAsync(contract.ClientId);
            return View(contract);
        }
        await _contractService.CreateAsync(contract);
        await _auditService.LogAsync("Création", "Contrat", contract.Id, contract.Title);
        TempData["Success"] = "Contrat créé avec succès.";
        return RedirectToAction(nameof(Index));
    }

    // GET: /Contract/Edit/5
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Edit(int id)
    {
        var contract = await _contractService.GetByIdAsync(id);
        if (contract == null) return NotFound();
        await PopulateClientsAsync(contract.ClientId);
        return View(contract);
    }

    // POST: /Contract/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Edit(int id, Contract contract)
    {
        if (id != contract.Id) return NotFound();
        if (!ModelState.IsValid)
        {
            await PopulateClientsAsync(contract.ClientId);
            return View(contract);
        }
        var diff = await _contractService.UpdateAsync(contract);
        await _auditService.LogAsync("Modification", "Contrat", contract.Id, contract.Title, diff);
        TempData["Success"] = "Contrat mis à jour.";
        return RedirectToAction(nameof(Index));
    }

    // GET: /Contract/Delete/5
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var contract = await _contractService.GetByIdAsync(id);
        if (contract == null) return NotFound();
        return View(contract);
    }

    // POST: /Contract/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var contract = await _contractService.GetByIdAsync(id);
        await _contractService.DeleteAsync(id);
        await _auditService.LogAsync("Suppression", "Contrat", id, contract?.Title);
        TempData["Success"] = "Contrat supprimé.";
        return RedirectToAction(nameof(Index));
    }

    // GET: /Contract/ExportCsv?search=&status=
    public async Task<IActionResult> ExportCsv(string? search, ContractStatus? status)
    {
        var contracts = await _contractService.GetFilteredAsync(search, status);

        var sb = new StringBuilder();
        sb.AppendLine("Id;Titre;Client;Montant;DateDebut;DateFin;Statut");
        foreach (var c in contracts)
        {
            sb.AppendLine(string.Join(';',
                c.Id,
                Escape(c.Title),
                Escape(c.Client?.Name ?? ""),
                c.Amount.ToString(CultureInfo.InvariantCulture),
                c.StartDate.ToString("yyyy-MM-dd"),
                c.EndDate.ToString("yyyy-MM-dd"),
                c.Status));
        }

        // BOM UTF-8 pour qu'Excel affiche correctement les accents.
        var bytes = Encoding.UTF8.GetPreamble()
            .Concat(Encoding.UTF8.GetBytes(sb.ToString()))
            .ToArray();

        var fileName = $"contrats_{DateTime.Now:yyyyMMdd_HHmm}.csv";
        return File(bytes, "text/csv", fileName);
    }

    private static string Escape(string value)
    {
        if (value.Contains(';') || value.Contains('"') || value.Contains('\n'))
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        return value;
    }

    private async Task PopulateClientsAsync(int? selectedId = null)
    {
        var clients = await _clientService.GetAllAsync();
        ViewBag.Clients = new SelectList(clients, "Id", "Name", selectedId);
    }
}
