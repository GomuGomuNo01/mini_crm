using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MiniCrm.Controllers;

public class HomeController : Controller
{
    // La page d'accueil redirige vers le dashboard (protégé par auth).
    public IActionResult Index() => RedirectToAction("Index", "Dashboard");

    [AllowAnonymous]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() => View();
}
