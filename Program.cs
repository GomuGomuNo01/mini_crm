using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MiniCrm.Data;
using MiniCrm.Models;
using MiniCrm.Services;

var builder = WebApplication.CreateBuilder(args);

// --- Base de données : MySQL (WAMP local) via le provider officiel Oracle ---
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Chaîne de connexion 'DefaultConnection' introuvable.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySQL(connectionString));

// --- ASP.NET Core Identity ---
builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;
        options.Password.RequireDigit = true;
        options.Password.RequiredLength = 8;
        options.Password.RequireNonAlphanumeric = false;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

// --- Services métier (pattern Interface + Service) ---
builder.Services.AddScoped<IClientService, ClientService>();
builder.Services.AddScoped<IContractService, ContractService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddHttpContextAccessor();

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

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
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
