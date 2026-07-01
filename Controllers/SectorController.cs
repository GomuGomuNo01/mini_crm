using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniCrm.Models;
using MiniCrm.Services;

namespace MiniCrm.Controllers;

[Authorize(Roles = "Admin")]
public class SectorController : Controller
{
    private readonly ISectorService _sectorService;

    public SectorController(ISectorService sectorService)
    {
        _sectorService = sectorService;
    }

    private bool IsAjax => Request.Headers["X-Requested-With"] == "XMLHttpRequest";

    // GET: /Sector
    public async Task<IActionResult> Index(string? search, int page = 1)
    {
        var result = await _sectorService.GetPagedAsync(search, page, pageSize: 8);
        ViewBag.Search = search;

        if (IsAjax) return PartialView("_SectorTable", result);
        return View(result);
    }

    // GET: /Sector/Create
    public IActionResult Create()
    {
        if (IsAjax) return PartialView("_SectorForm", new Sector());
        return RedirectToAction(nameof(Index));
    }

    // POST: /Sector/Create
    // Note : l'audit est journalisé automatiquement par AuditSaveChangesInterceptor.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Sector sector)
    {
        var (ok, error) = await _sectorService.CreateAsync(sector.Name);
        if (!ok)
        {
            ModelState.AddModelError(nameof(Sector.Name), error!);
            return IsAjax ? PartialView("_SectorForm", sector) : RedirectToAction(nameof(Index));
        }

        if (IsAjax) return Json(new { success = true, message = $"Secteur « {sector.Name.Trim()} » ajouté." });
        return RedirectToAction(nameof(Index));
    }

    // GET: /Sector/Edit/5
    public async Task<IActionResult> Edit(int id)
    {
        var sector = await _sectorService.GetByIdAsync(id);
        if (sector == null) return NotFound();
        if (IsAjax) return PartialView("_SectorForm", sector);
        return RedirectToAction(nameof(Index));
    }

    // POST: /Sector/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Sector sector)
    {
        var (ok, error, _) = await _sectorService.UpdateAsync(id, sector.Name);
        if (!ok)
        {
            ModelState.AddModelError(nameof(Sector.Name), error!);
            return IsAjax ? PartialView("_SectorForm", sector) : RedirectToAction(nameof(Index));
        }

        if (IsAjax) return Json(new { success = true, message = "Secteur mis à jour." });
        return RedirectToAction(nameof(Index));
    }

    // GET: /Sector/Delete/5
    public async Task<IActionResult> Delete(int id)
    {
        var sector = await _sectorService.GetByIdAsync(id);
        if (sector == null) return NotFound();
        ViewBag.ClientCount = await _sectorService.GetClientCountAsync(sector.Name);
        if (IsAjax) return PartialView("_SectorDeleteConfirm", sector);
        return RedirectToAction(nameof(Index));
    }

    // POST: /Sector/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var name = await _sectorService.DeleteAsync(id);
        if (IsAjax) return Json(new { success = true, message = $"Secteur « {name} » supprimé." });
        return RedirectToAction(nameof(Index));
    }
}
