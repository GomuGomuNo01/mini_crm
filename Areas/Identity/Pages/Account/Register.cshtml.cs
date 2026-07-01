using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MiniCrm.Areas.Identity.Pages.Account;

// L'inscription publique est désactivée : seuls les administrateurs créent des
// comptes (voir UserController). Cette page neutralise la route /Account/Register
// fournie par défaut par le framework Identity.
[AllowAnonymous]
public class RegisterModel : PageModel
{
    public IActionResult OnGet() => RedirectToPage("./Login");
    public IActionResult OnPost() => RedirectToPage("./Login");
}
