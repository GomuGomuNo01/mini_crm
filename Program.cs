using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MiniCrm.Data;
using MiniCrm.Models;
using MiniCrm.Services;

var builder = WebApplication.CreateBuilder(args);

// --- Base de données : MySQL (WAMP local) via le provider officiel Oracle ---
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Chaîne de connexion 'DefaultConnection' introuvable.");

builder.Services.AddHttpContextAccessor();
// Scoped (pas Singleton) : l'intercepteur garde un état par requête (_pending),
// il doit donc vivre dans le même scope que le DbContext.
builder.Services.AddScoped<AuditSaveChangesInterceptor>();

builder.Services.AddDbContext<ApplicationDbContext>((sp, options) =>
    options.UseMySQL(connectionString)
        .AddInterceptors(sp.GetRequiredService<AuditSaveChangesInterceptor>()));

// --- ASP.NET Core Identity ---
builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;
        options.Password.RequireDigit = true;
        options.Password.RequiredLength = 8;
        options.Password.RequireNonAlphanumeric = false;

        // Sécurité : verrouillage temporaire après 5 tentatives échouées (anti-force brute).
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        options.Lockout.AllowedForNewUsers = true;

        // Caractères autorisés pour le nom d'utilisateur (défense en profondeur).
        options.User.RequireUniqueEmail = true;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

// --- Services métier (pattern Interface + Service) ---
builder.Services.AddScoped<IClientService, ClientService>();
builder.Services.AddScoped<IContractService, ContractService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<ISectorService, SectorService>();

// --- MVC + protection globale des routes ---
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages(); // pages Identity (Login/Register/Logout)

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

var app = builder.Build();

// Culture française (dates, nombres, monnaie) pour toute l'application.
var frCulture = new System.Globalization.CultureInfo("fr-FR");
System.Globalization.CultureInfo.DefaultThreadCurrentCulture = frCulture;
System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = frCulture;

// --- Reverse proxy / ngrok : tenir compte du schéma (https) et de l'IP transmis
//     par le proxy, sinon les redirections et cookies sécurisés sont incorrects. ---
var forwardedOptions = new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
};
forwardedOptions.KnownIPNetworks.Clear();
forwardedOptions.KnownProxies.Clear();
app.UseForwardedHeaders(forwardedOptions);

// --- En-têtes de sécurité HTTP (défense en profondeur) ---
app.Use(async (context, next) =>
{
    var headers = context.Response.Headers;
    headers["X-Content-Type-Options"] = "nosniff";          // anti MIME-sniffing
    headers["X-Frame-Options"] = "DENY";                    // anti clickjacking
    headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    headers["X-Permitted-Cross-Domain-Policies"] = "none";
    headers["Content-Security-Policy"] =
        "default-src 'self'; " +
        "script-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net; " +
        "style-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net https://fonts.googleapis.com; " +
        "font-src 'self' https://fonts.gstatic.com https://cdn.jsdelivr.net data:; " +
        "img-src 'self' data:; " +
        "object-src 'none'; base-uri 'self'; frame-ancestors 'none'";
    await next();
});

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
    // Redirection HTTPS uniquement hors développement (en local on tourne en HTTP).
    app.UseHttpsRedirection();
}

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// Les fichiers statiques (CSS/JS/images) doivent rester accessibles sans
// authentification, sinon la FallbackPolicy les redirige aussi vers le login.
app.MapStaticAssets().AllowAnonymous();

app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapRazorPages();

// --- Seed : migrations + rôles + admin + données de démo ---
using (var scope = app.Services.CreateScope())
{
    await SeedData.InitializeAsync(scope.ServiceProvider);
}

app.Run();
