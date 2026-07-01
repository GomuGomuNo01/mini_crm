using MiniCrm.Models;
using MiniCrm.Services;
using MiniCrm.Tests.TestHelpers;

namespace MiniCrm.Tests.Services;

public class ClientServiceTests
{
    [Fact]
    public async Task CreateAsync_PersistsClient_AndSetsCreatedAt()
    {
        using var context = TestDbContextFactory.Create();
        var service = new ClientService(context);
        var client = new Client { Name = "Acme", Email = "acme@test.fr", Sector = "Informatique" };

        await service.CreateAsync(client);

        var saved = await service.GetByIdAsync(client.Id);
        Assert.NotNull(saved);
        Assert.Equal("Acme", saved!.Name);
        Assert.True(saved.CreatedAt > DateTime.UtcNow.AddMinutes(-1));
    }

    [Fact]
    public async Task CreateAsync_WithNewSector_AddsSectorToCatalog()
    {
        using var context = TestDbContextFactory.Create();
        var service = new ClientService(context);

        await service.CreateAsync(new Client { Name = "Client A", Email = "a@test.fr", Sector = "Domotique" });

        var sectors = await service.GetSectorsAsync();
        Assert.Contains("Domotique", sectors);
    }

    [Fact]
    public async Task CreateAsync_WithExistingSector_DoesNotDuplicateCatalogEntry()
    {
        using var context = TestDbContextFactory.Create();
        context.Sectors.Add(new Sector { Name = "Informatique" });
        await context.SaveChangesAsync();
        var service = new ClientService(context);

        // Casse différente : ne doit pas créer un doublon "informatique".
        await service.CreateAsync(new Client { Name = "Client B", Email = "b@test.fr", Sector = "informatique" });

        Assert.Equal(1, context.Sectors.Count());
    }

    [Fact]
    public async Task CreateAsync_WithoutSector_DoesNotTouchCatalog()
    {
        using var context = TestDbContextFactory.Create();
        var service = new ClientService(context);

        await service.CreateAsync(new Client { Name = "Client C", Email = "c@test.fr", Sector = null });

        Assert.Empty(context.Sectors);
    }

    [Fact]
    public async Task GetPagedAsync_FiltersBySearch_AcrossNameEmailAndSector()
    {
        using var context = TestDbContextFactory.Create();
        var service = new ClientService(context);
        await service.CreateAsync(new Client { Name = "TechNova", Email = "hello@technova.io", Sector = "Informatique" });
        await service.CreateAsync(new Client { Name = "Boulangerie Martin", Email = "contact@boulangerie.fr", Sector = "Agroalimentaire" });

        var result = await service.GetPagedAsync(search: "tech");

        Assert.Equal(1, result.TotalCount);
        Assert.Equal("TechNova", result.Items[0].Name);
    }

    [Fact]
    public async Task GetPagedAsync_RespectsPageSize_AndReturnsCorrectTotalPages()
    {
        using var context = TestDbContextFactory.Create();
        var service = new ClientService(context);
        for (var i = 0; i < 5; i++)
            await service.CreateAsync(new Client { Name = $"Client {i}", Email = $"c{i}@test.fr" });

        var page1 = await service.GetPagedAsync(page: 1, pageSize: 2);
        var page3 = await service.GetPagedAsync(page: 3, pageSize: 2);

        Assert.Equal(5, page1.TotalCount);
        Assert.Equal(2, page1.Items.Count);
        Assert.Equal(3, page1.TotalPages);
        Assert.Single(page3.Items); // dernière page : 1 seul élément restant
        Assert.False(page3.HasNext);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesFields_WhenClientExists()
    {
        using var context = TestDbContextFactory.Create();
        var service = new ClientService(context);
        var client = new Client { Name = "Old Name", Email = "old@test.fr", Status = ClientStatus.Prospect };
        await service.CreateAsync(client);

        client.Name = "New Name";
        client.Status = ClientStatus.Active;
        await service.UpdateAsync(client);

        var updated = await service.GetByIdAsync(client.Id);
        Assert.Equal("New Name", updated!.Name);
        Assert.Equal(ClientStatus.Active, updated.Status);
    }

    [Fact]
    public async Task UpdateAsync_DoesNothing_WhenClientDoesNotExist()
    {
        using var context = TestDbContextFactory.Create();
        var service = new ClientService(context);

        // Ne doit pas lever d'exception pour un client inexistant.
        await service.UpdateAsync(new Client { Id = 999, Name = "Ghost" });

        Assert.Empty(context.Clients);
    }

    [Fact]
    public async Task DeleteAsync_RemovesClient()
    {
        using var context = TestDbContextFactory.Create();
        var service = new ClientService(context);
        var client = new Client { Name = "To Delete", Email = "del@test.fr" };
        await service.CreateAsync(client);

        await service.DeleteAsync(client.Id);

        Assert.Null(await service.GetByIdAsync(client.Id));
    }

    [Fact]
    public async Task GetSectorsAsync_ReturnsNamesOrderedAlphabetically()
    {
        using var context = TestDbContextFactory.Create();
        context.Sectors.AddRange(
            new Sector { Name = "Transport" },
            new Sector { Name = "Automobile" },
            new Sector { Name = "Commerce" });
        await context.SaveChangesAsync();
        var service = new ClientService(context);

        var sectors = await service.GetSectorsAsync();

        Assert.Equal(new[] { "Automobile", "Commerce", "Transport" }, sectors);
    }
}
