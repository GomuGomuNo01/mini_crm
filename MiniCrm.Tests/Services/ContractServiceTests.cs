using MiniCrm.Data;
using MiniCrm.Models;
using MiniCrm.Services;
using MiniCrm.Tests.TestHelpers;

namespace MiniCrm.Tests.Services;

public class ContractServiceTests
{
    private static async Task<Client> SeedClientAsync(ApplicationDbContext context, string name = "Client Test")
    {
        var client = new Client { Name = name, Email = $"{Guid.NewGuid()}@test.fr" };
        context.Clients.Add(client);
        await context.SaveChangesAsync();
        return client;
    }

    [Fact]
    public async Task CreateAsync_PersistsContract()
    {
        using var context = TestDbContextFactory.Create();
        var client = await SeedClientAsync(context);
        var service = new ContractService(context);

        var contract = new Contract { Title = "Contrat A", Amount = 1000, ClientId = client.Id };
        await service.CreateAsync(contract);

        var saved = await service.GetByIdAsync(contract.Id);
        Assert.NotNull(saved);
        Assert.Equal("Contrat A", saved!.Title);
    }

    [Fact]
    public async Task GetPagedAsync_FiltersByStatus()
    {
        using var context = TestDbContextFactory.Create();
        var client = await SeedClientAsync(context);
        var service = new ContractService(context);
        await service.CreateAsync(new Contract { Title = "Actif", ClientId = client.Id, Status = ContractStatus.Active });
        await service.CreateAsync(new Contract { Title = "Brouillon", ClientId = client.Id, Status = ContractStatus.Draft });

        var result = await service.GetPagedAsync(status: ContractStatus.Draft);

        Assert.Equal(1, result.TotalCount);
        Assert.Equal("Brouillon", result.Items[0].Title);
    }

    [Fact]
    public async Task GetPagedAsync_FiltersBySearch_OnTitleOrClientName()
    {
        using var context = TestDbContextFactory.Create();
        var techClient = await SeedClientAsync(context, "TechNova SARL");
        var otherClient = await SeedClientAsync(context, "Boulangerie Martin");
        var service = new ContractService(context);
        await service.CreateAsync(new Contract { Title = "Maintenance", ClientId = techClient.Id });
        await service.CreateAsync(new Contract { Title = "Audit sécurité", ClientId = otherClient.Id });

        var byTitle = await service.GetPagedAsync(search: "Maintenance");
        var byClient = await service.GetPagedAsync(search: "Boulangerie");

        Assert.Equal(1, byTitle.TotalCount);
        Assert.Equal(1, byClient.TotalCount);
        Assert.Equal("Audit sécurité", byClient.Items[0].Title);
    }

    [Fact]
    public async Task GetPagedAsync_CombinesSearchAndStatus()
    {
        using var context = TestDbContextFactory.Create();
        var client = await SeedClientAsync(context);
        var service = new ContractService(context);
        await service.CreateAsync(new Contract { Title = "Maintenance web", ClientId = client.Id, Status = ContractStatus.Active });
        await service.CreateAsync(new Contract { Title = "Maintenance véhicules", ClientId = client.Id, Status = ContractStatus.Expired });

        var result = await service.GetPagedAsync(search: "Maintenance", status: ContractStatus.Active);

        Assert.Equal(1, result.TotalCount);
        Assert.Equal("Maintenance web", result.Items[0].Title);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesContractFields()
    {
        using var context = TestDbContextFactory.Create();
        var client = await SeedClientAsync(context);
        var service = new ContractService(context);
        var contract = new Contract { Title = "Old Title", Amount = 100, ClientId = client.Id };
        await service.CreateAsync(contract);

        contract.Title = "New Title";
        contract.Amount = 200;
        contract.Status = ContractStatus.Cancelled;
        await service.UpdateAsync(contract);

        var updated = await service.GetByIdAsync(contract.Id);
        Assert.Equal("New Title", updated!.Title);
        Assert.Equal(200, updated.Amount);
        Assert.Equal(ContractStatus.Cancelled, updated.Status);
    }

    [Fact]
    public async Task DeleteAsync_RemovesContract()
    {
        using var context = TestDbContextFactory.Create();
        var client = await SeedClientAsync(context);
        var service = new ContractService(context);
        var contract = new Contract { Title = "To Delete", ClientId = client.Id };
        await service.CreateAsync(contract);

        await service.DeleteAsync(contract.Id);

        Assert.Null(await service.GetByIdAsync(contract.Id));
    }

    [Fact]
    public async Task GetFilteredAsync_ReturnsAllMatchingContracts_ForExport()
    {
        using var context = TestDbContextFactory.Create();
        var client = await SeedClientAsync(context);
        var service = new ContractService(context);
        for (var i = 0; i < 3; i++)
            await service.CreateAsync(new Contract { Title = $"Contrat {i}", ClientId = client.Id, Status = ContractStatus.Active });

        var all = await service.GetFilteredAsync(status: ContractStatus.Active);

        Assert.Equal(3, all.Count());
    }
}
