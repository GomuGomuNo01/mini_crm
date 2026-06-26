using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniCrm.Models;
using MiniCrm.Services;
using MiniCrm.ViewModels;

namespace MiniCrm.Controllers;

[Authorize]
public class ClientController : Controller
{
    private readonly IClientService _clientService;
    private readonly IAuditService _auditService;

    public ClientController(IClientService clientService, IAuditService auditService)
    {
        _clientService = clientService;
        _auditService = auditService;
    }

    // GET: /Client
    public async Task<IActionResult> Index(string? search)
    {
        var clients = await _clientService.GetAllAsync(search);
        var vm = new ClientViewModel { Clients = clients, Search = search };
        return View(vm);
    }

    // GET: /Client/Details/5
    public async Task<IActionResult> Details(int id)
    {
        var client = await _clientService.GetByIdAsync(id);
        if (client == null) return NotFound();
        return View(client);
    }

    // GET: /Client/Create
    [Authorize(Roles = "Admin")]
    public IActionResult Create() => View(new Client());

    // POST: /Client/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create(Client client)
    {
        if (!ModelState.IsValid) return View(client);
        await _clientService.CreateAsync(client);
        await _auditService.LogAsync("Création", "Client", client.Id, client.Name);
        TempData["Success"] = "Client créé avec succès.";
        return RedirectToAction(nameof(Index));
    }

    // GET: /Client/Edit/5
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Edit(int id)
    {
        var client = await _clientService.GetByIdAsync(id);
        if (client == null) return NotFound();
        return View(client);
    }

    // POST: /Client/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Edit(int id, Client client)
    {
        if (id != client.Id) return NotFound();
        if (!ModelState.IsValid) return View(client);
        var diff = await _clientService.UpdateAsync(client);
        await _auditService.LogAsync("Modification", "Client", client.Id, client.Name, diff);
        TempData["Success"] = "Client mis à jour.";
        return RedirectToAction(nameof(Index));
    }

    // GET: /Client/Delete/5
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var client = await _clientService.GetByIdAsync(id);
        if (client == null) return NotFound();
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
        await _auditService.LogAsync("Suppression", "Client", id, client?.Name);
        TempData["Success"] = "Client supprimé.";
        return RedirectToAction(nameof(Index));
    }
}
