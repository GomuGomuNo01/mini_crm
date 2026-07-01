using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace MiniCrm.Tests.TestHelpers;

public static class FakeHttpContextAccessor
{
    // Simule un utilisateur connecté (nécessaire pour que l'intercepteur d'audit
    // et AuditService renseignent l'auteur de l'action).
    public static IHttpContextAccessor Authenticated(string userName, string role = "Admin")
    {
        var identity = new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.Name, userName), new Claim(ClaimTypes.Role, role) },
            authenticationType: "TestAuth");

        return new HttpContextAccessor { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) } };
    }

    // Simule une requête sans utilisateur authentifié (ex. seed au démarrage).
    public static IHttpContextAccessor Anonymous() =>
        new HttpContextAccessor { HttpContext = new DefaultHttpContext() };
}
