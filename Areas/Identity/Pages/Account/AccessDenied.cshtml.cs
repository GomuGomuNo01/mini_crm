using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MiniCrm.Areas.Identity.Pages.Account;

// Remplace la page « Access denied » par défaut d'Identity par une version
// intégrée à l'application (même navigation, même langage visuel).
[AllowAnonymous]
public class AccessDeniedModel : PageModel
{
    public void OnGet() { }
}
