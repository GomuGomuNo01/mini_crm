using System.Globalization;
using MiniCrm.Data;
using MiniCrm.Models;
using MiniCrm.Tests.TestHelpers;

namespace MiniCrm.Tests.Data;

public class AuditSaveChangesInterceptorTests
{
    [Fact]
    public async Task ClientCreation_WritesSingleAuditEntry_WithActionCreation()
    {
        var interceptor = new AuditSaveChangesInterceptor(FakeHttpContextAccessor.Authenticated("admin@minicrm.dev"));
        using var context = TestDbContextFactory.CreateWithInterceptor(interceptor);

        context.Clients.Add(new Client { Name = "Acme", Email = "acme@test.fr" });
        await context.SaveChangesAsync();

        var log = context.AuditLogs.Single();
        Assert.Equal("Création", log.Action);
        Assert.Equal("Client", log.EntityType);
        Assert.Equal("Acme", log.EntityLabel);
        Assert.Equal("admin@minicrm.dev", log.UserName);
        Assert.True(log.EntityId > 0); // id auto-généré, disponible après insertion
    }

    [Fact]
    public async Task ClientAndAutoCreatedSector_BothAuditedInSameSaveChanges()
    {
        // Reproduit le scénario ClientService.CreateAsync : le client et son
        // nouveau secteur sont ajoutés puis sauvegardés en un seul SaveChanges.
        var interceptor = new AuditSaveChangesInterceptor(FakeHttpContextAccessor.Authenticated("admin@minicrm.dev"));
        using var context = TestDbContextFactory.CreateWithInterceptor(interceptor);

        context.Sectors.Add(new Sector { Name = "Domotique" });
        context.Clients.Add(new Client { Name = "Client X", Email = "x@test.fr", Sector = "Domotique" });
        await context.SaveChangesAsync();

        Assert.Equal(2, context.AuditLogs.Count());
        Assert.Contains(context.AuditLogs, l => l.EntityType == "Client" && l.EntityLabel == "Client X");
        Assert.Contains(context.AuditLogs, l => l.EntityType == "Secteur" && l.EntityLabel == "Domotique");
    }

    [Fact]
    public async Task ClientModification_WritesDetails_OneLinePerChangedField()
    {
        var interceptor = new AuditSaveChangesInterceptor(FakeHttpContextAccessor.Authenticated("admin@minicrm.dev"));
        using var context = TestDbContextFactory.CreateWithInterceptor(interceptor);
        var client = new Client { Name = "client_test", Sector = "Automobile", Status = ClientStatus.Prospect, Email = "c@test.fr" };
        context.Clients.Add(client);
        await context.SaveChangesAsync();

        client.Name = "client_test_mod";
        client.Sector = "Bâtiment";
        client.Status = ClientStatus.Active;
        await context.SaveChangesAsync();

        var log = context.AuditLogs.OrderByDescending(l => l.Timestamp).First();
        Assert.Equal("Modification", log.Action);
        Assert.NotNull(log.Details);
        var lines = log.Details!.Split('\n');
        Assert.Equal(3, lines.Length);
        Assert.Contains("Nom : « client_test » → « client_test_mod »", lines);
        Assert.Contains("Secteur : « Automobile » → « Bâtiment »", lines);
        Assert.Contains("Statut : « Prospect » → « Active »", lines);
    }

    [Fact]
    public async Task ClientModification_WithNoRelevantFieldChanged_DoesNotWriteAuditEntry()
    {
        var interceptor = new AuditSaveChangesInterceptor(FakeHttpContextAccessor.Authenticated("admin@minicrm.dev"));
        using var context = TestDbContextFactory.CreateWithInterceptor(interceptor);
        var client = new Client { Name = "Stable", Email = "stable@test.fr" };
        context.Clients.Add(client);
        await context.SaveChangesAsync();
        var afterCreate = context.AuditLogs.Count();

        // Marque l'entité comme modifiée sans changer de valeur réelle.
        context.Entry(client).Property(c => c.Name).IsModified = true;
        await context.SaveChangesAsync();

        Assert.Equal(afterCreate, context.AuditLogs.Count());
    }

    [Fact]
    public async Task ClientDeletion_WritesAuditEntry_WithActionSuppression()
    {
        var interceptor = new AuditSaveChangesInterceptor(FakeHttpContextAccessor.Authenticated("admin@minicrm.dev"));
        using var context = TestDbContextFactory.CreateWithInterceptor(interceptor);
        var client = new Client { Name = "À supprimer", Email = "sup@test.fr" };
        context.Clients.Add(client);
        await context.SaveChangesAsync();

        context.Clients.Remove(client);
        await context.SaveChangesAsync();

        var log = context.AuditLogs.OrderByDescending(l => l.Timestamp).First();
        Assert.Equal("Suppression", log.Action);
        Assert.Equal("À supprimer", log.EntityLabel);
    }

    [Fact]
    public async Task ContractClientChange_LogsClientNamesInsteadOfIds()
    {
        var interceptor = new AuditSaveChangesInterceptor(FakeHttpContextAccessor.Authenticated("admin@minicrm.dev"));
        using var context = TestDbContextFactory.CreateWithInterceptor(interceptor);
        var clientA = new Client { Name = "Client A", Email = "a@test.fr" };
        var clientB = new Client { Name = "Client B", Email = "b@test.fr" };
        context.Clients.AddRange(clientA, clientB);
        await context.SaveChangesAsync(); // les clients doivent avoir leur Id généré avant d'être référencés

        var contract = new Contract { Title = "Contrat", ClientId = clientA.Id };
        context.Contracts.Add(contract);
        await context.SaveChangesAsync();

        contract.ClientId = clientB.Id;
        await context.SaveChangesAsync();

        var log = context.AuditLogs.OrderByDescending(l => l.Timestamp).First();
        Assert.Equal("Contrat", log.EntityType);
        Assert.Contains("Client : « Client A » → « Client B »", log.Details);
    }

    [Fact]
    public async Task ContractAmountChange_FormatsAmountAsCurrency()
    {
        var interceptor = new AuditSaveChangesInterceptor(FakeHttpContextAccessor.Authenticated("admin@minicrm.dev"));
        using var context = TestDbContextFactory.CreateWithInterceptor(interceptor);
        var client = new Client { Name = "Client", Email = "c@test.fr" };
        context.Clients.Add(client);
        await context.SaveChangesAsync();

        var contract = new Contract { Title = "Contrat", ClientId = client.Id, Amount = 1000m };
        context.Contracts.Add(contract);
        await context.SaveChangesAsync();

        contract.Amount = 2500.50m;
        await context.SaveChangesAsync();

        // Formate les montants attendus avec la même culture (fr-FR) que l'intercepteur,
        // pour ne pas dépendre du caractère exact utilisé comme séparateur de milliers.
        var frCulture = CultureInfo.GetCultureInfo("fr-FR");
        var log = context.AuditLogs.OrderByDescending(l => l.Timestamp).First();
        Assert.Contains(1000m.ToString("N2", frCulture) + " €", log.Details);
        Assert.Contains(2500.50m.ToString("N2", frCulture) + " €", log.Details);
    }

    [Fact]
    public async Task NoAuthenticatedUser_DoesNotWriteAnyAuditEntry()
    {
        // Reproduit le seed au démarrage de l'application : pas de HttpContext.User authentifié.
        var interceptor = new AuditSaveChangesInterceptor(FakeHttpContextAccessor.Anonymous());
        using var context = TestDbContextFactory.CreateWithInterceptor(interceptor);

        context.Clients.Add(new Client { Name = "Seed Client", Email = "seed@test.fr" });
        await context.SaveChangesAsync();

        Assert.Empty(context.AuditLogs);
    }

    [Fact]
    public async Task UnrelatedEntityChanges_AreNotAudited()
    {
        var interceptor = new AuditSaveChangesInterceptor(FakeHttpContextAccessor.Authenticated("admin@minicrm.dev"));
        using var context = TestDbContextFactory.CreateWithInterceptor(interceptor);

        // ApplicationUser (Identity) n'est pas dans le périmètre de l'audit automatique.
        context.Users.Add(new ApplicationUser { UserName = "test@user.fr", Email = "test@user.fr" });
        await context.SaveChangesAsync();

        Assert.Empty(context.AuditLogs);
    }

    [Fact]
    public async Task WritingAuditLog_DoesNotTriggerRecursiveAuditing()
    {
        // Vérifie que l'écriture de l'AuditLog (qui déclenche elle-même un SaveChanges)
        // ne provoque pas de boucle infinie ni de doublons.
        var interceptor = new AuditSaveChangesInterceptor(FakeHttpContextAccessor.Authenticated("admin@minicrm.dev"));
        using var context = TestDbContextFactory.CreateWithInterceptor(interceptor);

        context.Clients.Add(new Client { Name = "Solo", Email = "solo@test.fr" });
        await context.SaveChangesAsync();

        Assert.Equal(1, context.AuditLogs.Count());
    }

    [Fact]
    public async Task SectorCreation_IsAudited()
    {
        var interceptor = new AuditSaveChangesInterceptor(FakeHttpContextAccessor.Authenticated("admin@minicrm.dev"));
        using var context = TestDbContextFactory.CreateWithInterceptor(interceptor);

        context.Sectors.Add(new Sector { Name = "Nouveau secteur" });
        await context.SaveChangesAsync();

        var log = context.AuditLogs.Single();
        Assert.Equal("Secteur", log.EntityType);
        Assert.Equal("Création", log.Action);
        Assert.Equal("Nouveau secteur", log.EntityLabel);
    }
}
