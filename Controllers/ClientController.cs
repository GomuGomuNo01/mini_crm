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

    public ClientController(IClientService clientService)
    {
        _clientService = clientService;
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
    public IActionResult Create() => View(new Client());

    // POST: /Client/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Client client)
    {
        if (!ModelState.IsValid) return View(client);
        await _clientService.CreateAsync(client);
        TempData["Success"] = "Client créé avec succès.";
        return RedirectToAction(nameof(Index));
    }

    // GET: /Client/Edit/5
    public async Task<IActionResult> Edit(int id)
    {
        var client = await _clientService.GetByIdAsync(id);
        if (client == null) return NotFound();
        return View(client);
    }

    // POST: /Client/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Client client)
    {
        if (id != client.Id) return NotFound();
        if (!ModelState.IsValid) return View(client);
        await _clientService.UpdateAsync(client);
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
        await _clientService.DeleteAsync(id);
        TempData["Success"] = "Client supprimé.";
        return RedirectToAction(nameof(Index));
    }
}
