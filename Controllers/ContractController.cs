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

    public ContractController(IContractService contractService, IClientService clientService)
    {
        _contractService = contractService;
        _clientService = clientService;
    }

    private bool IsAjax => Request.Headers["X-Requested-With"] == "XMLHttpRequest";

    // GET: /Contract
    public async Task<IActionResult> Index(string? search, ContractStatus? status, int page = 1)
    {
        var result = await _contractService.GetPagedAsync(search, status, page, pageSize: 10);

        ViewBag.Search = search;
        ViewBag.Status = status;

        // Requête AJAX (filtre dynamique) : renvoyer uniquement le tableau + pagination.
        if (IsAjax)
            return PartialView("_ContractTable", result);

        return View(result);
    }

    // GET: /Contract/Details/5 (panneau latéral en lecture seule)
    public async Task<IActionResult> Details(int id)
    {
        var contract = await _contractService.GetByIdAsync(id);
        if (contract == null) return NotFound();

        if (IsAjax) return PartialView("_ContractDetails", contract);
        return View(contract);
    }

    // GET: /Contract/Create (formulaire dans un modal)
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create()
    {
        await PopulateClientsAsync();
        if (IsAjax) return PartialView("_ContractForm", new Contract());
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
            return IsAjax ? PartialView("_ContractForm", contract) : View(contract);
        }

        await _contractService.CreateAsync(contract);

        if (IsAjax) return Json(new { success = true, message = $"Contrat « {contract.Title} » créé avec succès." });

        TempData["Success"] = "Contrat créé avec succès.";
        return RedirectToAction(nameof(Index));
    }

    // GET: /Contract/Edit/5 (formulaire dans le panneau latéral)
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Edit(int id)
    {
        var contract = await _contractService.GetByIdAsync(id);
        if (contract == null) return NotFound();

        await PopulateClientsAsync(contract.ClientId);
        if (IsAjax) return PartialView("_ContractForm", contract);
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
            return IsAjax ? PartialView("_ContractForm", contract) : View(contract);
        }

        await _contractService.UpdateAsync(contract);

        if (IsAjax) return Json(new { success = true, message = $"Contrat « {contract.Title} » mis à jour." });

        TempData["Success"] = "Contrat mis à jour.";
        return RedirectToAction(nameof(Index));
    }

    // GET: /Contract/Delete/5 (confirmation dans un modal)
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var contract = await _contractService.GetByIdAsync(id);
        if (contract == null) return NotFound();

        if (IsAjax) return PartialView("_ContractDeleteConfirm", contract);
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

        if (IsAjax) return Json(new { success = true, message = $"Contrat « {contract?.Title} » supprimé." });

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

    private static string Escape(string? value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;

        // Anti CSV/Formula injection (CWE-1236) : une cellule commençant par
        // = + - @ (ou tab/CR) est interprétée comme une formule par Excel/Sheets.
        // On la neutralise en la préfixant d'une apostrophe.
        if ("=+-@\t\r".IndexOf(value[0]) >= 0)
            value = "'" + value;

        if (value.Contains(';') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
            return "\"" + value.Replace("\"", "\"\"") + "\"";

        return value;
    }

    private async Task PopulateClientsAsync(int? selectedId = null)
    {
        var clients = await _clientService.GetAllAsync();
        ViewBag.Clients = new SelectList(clients, "Id", "Name", selectedId);
    }
}
