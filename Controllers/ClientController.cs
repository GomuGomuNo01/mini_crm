using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniCrm.Models;
using MiniCrm.Services;

namespace MiniCrm.Controllers;

[Authorize]
public class ClientController : Controller
{
    private readonly IClientService _clientService;

    public ClientController(IClientService clientService)
    {
        _clientService = clientService;
    }

    private bool IsAjax => Request.Headers["X-Requested-With"] == "XMLHttpRequest";

    private async Task PopulateSectorsAsync() => ViewBag.Sectors = await _clientService.GetSectorsAsync();

    // GET: /Client
    public async Task<IActionResult> Index(string? search, int page = 1)
    {
        var result = await _clientService.GetPagedAsync(search, page, pageSize: 8);
        ViewBag.Search = search;

        // Requête AJAX (filtre dynamique / pagination) : renvoyer uniquement le tableau.
        if (IsAjax)
            return PartialView("_ClientTable", result);

        return View(result);
    }

    // GET: /Client/Details/5 (panneau latéral en lecture seule)
    public async Task<IActionResult> Details(int id)
    {
        var client = await _clientService.GetByIdAsync(id);
        if (client == null) return NotFound();

        if (IsAjax) return PartialView("_ClientDetails", client);
        return View(client);
    }

    // GET: /Client/Create (formulaire dans un modal)
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create()
    {
        await PopulateSectorsAsync();
        if (IsAjax) return PartialView("_ClientForm", new Client());
        return View(new Client());
    }

    // POST: /Client/Create
    // Note : l'audit (création/modification/suppression) est journalisé automatiquement
    // par AuditSaveChangesInterceptor à chaque SaveChanges, pas d'appel manuel ici.
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create(Client client)
    {
        if (!ModelState.IsValid)
        {
            await PopulateSectorsAsync();
            return IsAjax ? PartialView("_ClientForm", client) : View(client);
        }

        await _clientService.CreateAsync(client);

        if (IsAjax) return Json(new { success = true, message = $"Client « {client.Name} » créé avec succès." });

        TempData["Success"] = "Client créé avec succès.";
        return RedirectToAction(nameof(Index));
    }

    // GET: /Client/Edit/5 (formulaire dans le panneau latéral)
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Edit(int id)
    {
        var client = await _clientService.GetByIdAsync(id);
        if (client == null) return NotFound();

        await PopulateSectorsAsync();
        if (IsAjax) return PartialView("_ClientForm", client);
        return View(client);
    }

    // POST: /Client/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Edit(int id, Client client)
    {
        if (id != client.Id) return NotFound();
        if (!ModelState.IsValid)
        {
            await PopulateSectorsAsync();
            return IsAjax ? PartialView("_ClientForm", client) : View(client);
        }

        await _clientService.UpdateAsync(client);

        if (IsAjax) return Json(new { success = true, message = $"Client « {client.Name} » mis à jour." });

        TempData["Success"] = "Client mis à jour.";
        return RedirectToAction(nameof(Index));
    }

    // GET: /Client/Delete/5 (confirmation dans un modal)
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var client = await _clientService.GetByIdAsync(id);
        if (client == null) return NotFound();

        if (IsAjax) return PartialView("_ClientDeleteConfirm", client);
        return View(client);
    }

    // POST: /Client/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var client = await _clientService.GetByIdAsync(id);
        await _clientService.DeleteAsync(id);

        if (IsAjax) return Json(new { success = true, message = $"Client « {client?.Name} » supprimé." });

        TempData["Success"] = "Client supprimé.";
        return RedirectToAction(nameof(Index));
    }
}
