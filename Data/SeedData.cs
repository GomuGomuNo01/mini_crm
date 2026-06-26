using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MiniCrm.Models;

namespace MiniCrm.Data;

public static class SeedData
{
    public static async Task InitializeAsync(IServiceProvider services)
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

        // Appliquer les migrations en attente automatiquement.
        await context.Database.MigrateAsync();

        // --- Rôles ---
        string[] roles = { "Admin", "User" };
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }

        // --- Utilisateur admin de démonstration ---
        var admin = await userManager.FindByEmailAsync("admin@minicrm.dev");
        if (admin == null)
        {
            admin = new ApplicationUser
            {
                UserName = "admin@minicrm.dev",
                Email = "admin@minicrm.dev",
                EmailConfirmed = true
            };
            await userManager.CreateAsync(admin, "Admin@1234");
            await userManager.AddToRoleAsync(admin, "Admin");
        }

        // --- Utilisateur standard de démonstration (rôle User) ---
        var user = await userManager.FindByEmailAsync("user@minicrm.dev");
        if (user == null)
        {
            user = new ApplicationUser
            {
                UserName = "user@minicrm.dev",
                Email = "user@minicrm.dev",
                EmailConfirmed = true
            };
            await userManager.CreateAsync(user, "User@1234");
            await userManager.AddToRoleAsync(user, "User");
        }

        // --- Données de démonstration (clients + contrats) ---
        await SeedClientsAndContractsAsync(context);
    }

    private static async Task SeedClientsAndContractsAsync(ApplicationDbContext context)
    {
        if (await context.Clients.AnyAsync())
            return; // déjà peuplé

        var now = DateTime.UtcNow;

        var clients = new List<Client>
        {
            new() { Name = "Boulangerie Martin",   Email = "contact@boulangerie-martin.fr", Sector = "Agroalimentaire", Status = ClientStatus.Active,   CreatedAt = now.AddDays(-120) },
            new() { Name = "TechNova SARL",         Email = "hello@technova.io",             Sector = "Informatique",    Status = ClientStatus.Active,   CreatedAt = now.AddDays(-95)  },
            new() { Name = "Cabinet Dupont & Associés", Email = "info@dupont-avocats.fr",    Sector = "Juridique",       Status = ClientStatus.Active,   CreatedAt = now.AddDays(-80)  },
            new() { Name = "Garage Central",        Email = "contact@garage-central.fr",     Sector = "Automobile",      Status = ClientStatus.Inactive, CreatedAt = now.AddDays(-200) },
            new() { Name = "Fleuriste Les Lilas",   Email = "lilas@fleurs.fr",               Sector = "Commerce",        Status = ClientStatus.Prospect, CreatedAt = now.AddDays(-15)  },
            new() { Name = "BTP Construct",         Email = "devis@btp-construct.fr",        Sector = "Bâtiment",        Status = ClientStatus.Active,   CreatedAt = now.AddDays(-60)  },
            new() { Name = "Pharmacie du Parc",     Email = "contact@pharma-parc.fr",        Sector = "Santé",           Status = ClientStatus.Active,   CreatedAt = now.AddDays(-45)  },
            new() { Name = "Studio Pixel",          Email = "studio@pixel.design",           Sector = "Communication",   Status = ClientStatus.Prospect, CreatedAt = now.AddDays(-8)   },
            new() { Name = "Transports Rapides",    Email = "logistique@trans-rapides.fr",   Sector = "Transport",       Status = ClientStatus.Active,   CreatedAt = now.AddDays(-150) },
            new() { Name = "Hôtel Belle Vue",       Email = "reservation@bellevue.fr",       Sector = "Hôtellerie",      Status = ClientStatus.Inactive, CreatedAt = now.AddDays(-300) },
        };

        context.Clients.AddRange(clients);
        await context.SaveChangesAsync();

        var contracts = new List<Contract>
        {
            // 3 contrats expirant sous 30 jours (Active + EndDate proche)
            new() { Title = "Maintenance site web",      Amount = 2400m,  StartDate = now.AddMonths(-11), EndDate = now.AddDays(12),  Status = ContractStatus.Active,    ClientId = clients[1].Id },
            new() { Title = "Contrat support annuel",    Amount = 5600m,  StartDate = now.AddMonths(-12), EndDate = now.AddDays(20),  Status = ContractStatus.Active,    ClientId = clients[2].Id },
            new() { Title = "Hébergement cloud",         Amount = 1800m,  StartDate = now.AddMonths(-11), EndDate = now.AddDays(28),  Status = ContractStatus.Active,    ClientId = clients[5].Id },

            // Contrats actifs (longue durée)
            new() { Title = "Licence logicielle",        Amount = 12000m, StartDate = now.AddMonths(-2),  EndDate = now.AddMonths(10), Status = ContractStatus.Active,    ClientId = clients[0].Id },
            new() { Title = "Prestation de conseil",     Amount = 8500m,  StartDate = now.AddMonths(-1),  EndDate = now.AddMonths(5),  Status = ContractStatus.Active,    ClientId = clients[1].Id },
            new() { Title = "Refonte identité visuelle", Amount = 6200m,  StartDate = now.AddMonths(-3),  EndDate = now.AddMonths(3),  Status = ContractStatus.Active,    ClientId = clients[7].Id },
            new() { Title = "Contrat de transport",      Amount = 15000m, StartDate = now.AddMonths(-4),  EndDate = now.AddMonths(8),  Status = ContractStatus.Active,    ClientId = clients[8].Id },
            new() { Title = "Audit de sécurité",         Amount = 4200m,  StartDate = now.AddMonths(-1),  EndDate = now.AddMonths(2),  Status = ContractStatus.Active,    ClientId = clients[6].Id },

            // Contrats expirés
            new() { Title = "Campagne publicitaire 2024",Amount = 3500m,  StartDate = now.AddMonths(-18), EndDate = now.AddMonths(-6), Status = ContractStatus.Expired,   ClientId = clients[0].Id },
            new() { Title = "Réparation flotte",         Amount = 2200m,  StartDate = now.AddMonths(-14), EndDate = now.AddMonths(-2), Status = ContractStatus.Expired,   ClientId = clients[3].Id },
            new() { Title = "Saison hôtelière 2024",     Amount = 9800m,  StartDate = now.AddMonths(-15), EndDate = now.AddMonths(-3), Status = ContractStatus.Expired,   ClientId = clients[9].Id },

            // Contrats annulés
            new() { Title = "Projet abandonné",          Amount = 4000m,  StartDate = now.AddMonths(-6),  EndDate = now.AddMonths(6),  Status = ContractStatus.Cancelled, ClientId = clients[4].Id },
            new() { Title = "Devis non signé",           Amount = 1500m,  StartDate = now.AddMonths(-2),  EndDate = now.AddMonths(4),  Status = ContractStatus.Cancelled, ClientId = clients[3].Id },

            // Brouillons
            new() { Title = "Proposition maintenance",   Amount = 3000m,  StartDate = now,                EndDate = now.AddMonths(12), Status = ContractStatus.Draft,     ClientId = clients[7].Id },
            new() { Title = "Offre hébergement premium", Amount = 5400m,  StartDate = now,                EndDate = now.AddMonths(12), Status = ContractStatus.Draft,     ClientId = clients[4].Id },

            // Contrats actifs supplémentaires
            new() { Title = "Formation équipe",          Amount = 2800m,  StartDate = now.AddMonths(-2),  EndDate = now.AddMonths(1),  Status = ContractStatus.Active,    ClientId = clients[2].Id },
            new() { Title = "Maintenance véhicules",     Amount = 3600m,  StartDate = now.AddMonths(-5),  EndDate = now.AddMonths(7),  Status = ContractStatus.Active,    ClientId = clients[8].Id },
            new() { Title = "Pack communication",        Amount = 7100m,  StartDate = now.AddMonths(-1),  EndDate = now.AddMonths(11), Status = ContractStatus.Active,    ClientId = clients[5].Id },
            new() { Title = "Suivi pharmaceutique",      Amount = 4900m,  StartDate = now.AddMonths(-3),  EndDate = now.AddMonths(9),  Status = ContractStatus.Active,    ClientId = clients[6].Id },
            new() { Title = "Contrat saisonnier",        Amount = 6700m,  StartDate = now.AddMonths(-1),  EndDate = now.AddMonths(6),  Status = ContractStatus.Active,    ClientId = clients[0].Id },
        };

        context.Contracts.AddRange(contracts);
        await context.SaveChangesAsync();
    }
}
