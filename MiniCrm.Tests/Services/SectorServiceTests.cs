using MiniCrm.Models;
using MiniCrm.Services;
using MiniCrm.Tests.TestHelpers;

namespace MiniCrm.Tests.Services;

public class SectorServiceTests
{
    [Fact]
    public async Task CreateAsync_AddsSector_WhenNameIsValid()
    {
        using var context = TestDbContextFactory.Create();
        var service = new SectorService(context);

        var (ok, error) = await service.CreateAsync("Informatique");

        Assert.True(ok);
        Assert.Null(error);
        Assert.Single(context.Sectors);
    }

    [Fact]
    public async Task CreateAsync_TrimsWhitespace()
    {
        using var context = TestDbContextFactory.Create();
        var service = new SectorService(context);

        await service.CreateAsync("  Santé  ");

        Assert.Equal("Santé", context.Sectors.Single().Name);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task CreateAsync_RejectsEmptyName(string? name)
    {
        using var context = TestDbContextFactory.Create();
        var service = new SectorService(context);

        var (ok, error) = await service.CreateAsync(name!);

        Assert.False(ok);
        Assert.NotNull(error);
        Assert.Empty(context.Sectors);
    }

    [Fact]
    public async Task CreateAsync_RejectsDuplicate_CaseInsensitive()
    {
        using var context = TestDbContextFactory.Create();
        var service = new SectorService(context);
        await service.CreateAsync("Commerce");

        var (ok, error) = await service.CreateAsync("commerce");

        Assert.False(ok);
        Assert.Equal("Ce secteur existe déjà.", error);
        Assert.Single(context.Sectors);
    }

    [Fact]
    public async Task UpdateAsync_RenamesSector_AndPropagatesToClients()
    {
        using var context = TestDbContextFactory.Create();
        var sector = new Sector { Name = "Automobile" };
        context.Sectors.Add(sector);
        context.Clients.Add(new Client { Name = "Garage X", Email = "g@test.fr", Sector = "Automobile" });
        await context.SaveChangesAsync();
        var service = new SectorService(context);

        var (ok, error, oldName) = await service.UpdateAsync(sector.Id, "Automobile & Transport");

        Assert.True(ok);
        Assert.Equal("Automobile", oldName);
        Assert.Equal("Automobile & Transport", context.Clients.Single().Sector);
    }

    [Fact]
    public async Task UpdateAsync_RejectsDuplicateName()
    {
        using var context = TestDbContextFactory.Create();
        context.Sectors.Add(new Sector { Name = "Juridique" });
        var toRename = new Sector { Name = "Commerce" };
        context.Sectors.Add(toRename);
        await context.SaveChangesAsync();
        var service = new SectorService(context);

        var (ok, error, _) = await service.UpdateAsync(toRename.Id, "Juridique");

        Assert.False(ok);
        Assert.NotNull(error);
    }

    [Fact]
    public async Task UpdateAsync_ReturnsError_WhenSectorNotFound()
    {
        using var context = TestDbContextFactory.Create();
        var service = new SectorService(context);

        var (ok, error, _) = await service.UpdateAsync(999, "Nouveau nom");

        Assert.False(ok);
        Assert.Equal("Secteur introuvable.", error);
    }

    [Fact]
    public async Task DeleteAsync_RemovesSector_AndReturnsItsName()
    {
        using var context = TestDbContextFactory.Create();
        var sector = new Sector { Name = "À supprimer" };
        context.Sectors.Add(sector);
        await context.SaveChangesAsync();
        var service = new SectorService(context);

        var name = await service.DeleteAsync(sector.Id);

        Assert.Equal("À supprimer", name);
        Assert.Empty(context.Sectors);
    }

    [Fact]
    public async Task GetPagedAsync_FiltersBySearch()
    {
        using var context = TestDbContextFactory.Create();
        context.Sectors.AddRange(new Sector { Name = "Informatique" }, new Sector { Name = "Juridique" });
        await context.SaveChangesAsync();
        var service = new SectorService(context);

        // Note : la casse doit correspondre — le provider InMemory utilisé dans ces
        // tests compare les chaînes de façon sensible à la casse (Contains), alors
        // que MySQL en production est insensible à la casse par défaut.
        var result = await service.GetPagedAsync(search: "Info");

        Assert.Equal(1, result.TotalCount);
        Assert.Equal("Informatique", result.Items[0].Name);
    }

    [Fact]
    public async Task GetClientCountAsync_CountsClientsUsingSector()
    {
        using var context = TestDbContextFactory.Create();
        context.Clients.AddRange(
            new Client { Name = "A", Email = "a@t.fr", Sector = "Santé" },
            new Client { Name = "B", Email = "b@t.fr", Sector = "Santé" },
            new Client { Name = "C", Email = "c@t.fr", Sector = "Commerce" });
        await context.SaveChangesAsync();
        var service = new SectorService(context);

        var count = await service.GetClientCountAsync("Santé");

        Assert.Equal(2, count);
    }
}
