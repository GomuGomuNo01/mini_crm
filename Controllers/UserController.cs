using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniCrm.Models;
using MiniCrm.Services;
using MiniCrm.ViewModels;

namespace MiniCrm.Controllers;

[Authorize(Roles = "Admin")]
public class UserController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAuditService _auditService;

    public UserController(UserManager<ApplicationUser> userManager, IAuditService auditService)
    {
        _userManager = userManager;
        _auditService = auditService;
    }

    private bool IsAjax => Request.Headers["X-Requested-With"] == "XMLHttpRequest";

    private async Task<List<UserListItem>> BuildListAsync()
    {
        var currentUserId = _userManager.GetUserId(User);
        var users = await _userManager.Users.OrderBy(u => u.Email).ToListAsync();

        var list = new List<UserListItem>();
        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            list.Add(new UserListItem
            {
                Id = user.Id,
                Email = user.Email ?? user.UserName ?? "Email inconnu",
                Role = roles.FirstOrDefault() ?? "Aucun rôle",
                IsCurrentUser = user.Id == currentUserId
            });
        }
        return list;
    }

    // GET: /User
    public async Task<IActionResult> Index()
    {
        var list = await BuildListAsync();
        if (IsAjax) return PartialView("_UserTable", list);
        return View(list);
    }

    // GET: /User/Create (modal)
    public IActionResult Create()
    {
        if (IsAjax) return PartialView("_UserForm", new CreateUserViewModel());
        return RedirectToAction(nameof(Index));
    }

    // POST: /User/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateUserViewModel model)
    {
        if (model.Role != "Admin" && model.Role != "User")
            ModelState.AddModelError(nameof(model.Role), "Rôle invalide.");

        if (ModelState.IsValid)
        {
            var existing = await _userManager.FindByEmailAsync(model.Email);
            if (existing != null)
                ModelState.AddModelError(nameof(model.Email), "Un utilisateur avec cet email existe déjà.");
        }

        if (ModelState.IsValid)
        {
            var user = new ApplicationUser { UserName = model.Email, Email = model.Email, EmailConfirmed = true };
            var result = await _userManager.CreateAsync(user, model.Password);
            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, model.Role);
                await _auditService.LogAsync("Création", "Utilisateur", null, model.Email, $"Rôle : {model.Role}");

                if (IsAjax) return Json(new { success = true, message = $"Utilisateur « {model.Email} » créé." });
                TempData["Success"] = $"Utilisateur « {model.Email} » créé avec succès.";
                return RedirectToAction(nameof(Index));
            }
            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);
        }

        return IsAjax ? PartialView("_UserForm", model) : View(model);
    }

    // GET: /User/Delete/{id} (modal de confirmation)
    public async Task<IActionResult> Delete(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();
        if (IsAjax) return PartialView("_UserDeleteConfirm", user);
        return RedirectToAction(nameof(Index));
    }

    // POST: /User/Delete
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();

        // Empêcher un admin de supprimer son propre compte.
        if (user.Id == _userManager.GetUserId(User))
        {
            if (IsAjax) return Json(new { success = false, message = "Vous ne pouvez pas supprimer votre propre compte." });
            TempData["Success"] = "Vous ne pouvez pas supprimer votre propre compte.";
            return RedirectToAction(nameof(Index));
        }

        var email = user.Email ?? user.UserName ?? id;
        await _userManager.DeleteAsync(user);
        await _auditService.LogAsync("Suppression", "Utilisateur", null, email);

        if (IsAjax) return Json(new { success = true, message = $"Utilisateur « {email} » supprimé." });
        TempData["Success"] = $"Utilisateur « {email} » supprimé.";
        return RedirectToAction(nameof(Index));
    }
}
