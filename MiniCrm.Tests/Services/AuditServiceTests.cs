using MiniCrm.Services;
using MiniCrm.Tests.TestHelpers;

namespace MiniCrm.Tests.Services;

public class AuditServiceTests
{
    [Fact]
    public async Task LogAsync_WritesEntry_WithAuthenticatedUserAndRole()
    {
        using var context = TestDbContextFactory.Create();
        var accessor = FakeHttpContextAccessor.Authenticated("admin@minicrm.dev", "Admin");
        var service = new AuditService(context, accessor);

        await service.LogAsync("Création", "Client", 1, "Acme");

        var log = context.AuditLogs.Single();
        Assert.Equal("admin@minicrm.dev", log.UserName);
        Assert.Equal("Admin", log.UserRole);
        Assert.Equal("Création", log.Action);
        Assert.Equal("Client", log.EntityType);
        Assert.Equal("Acme", log.EntityLabel);
    }

    [Fact]
    public async Task LogAsync_UsesSysteme_WhenNoAuthenticatedUser()
    {
        using var context = TestDbContextFactory.Create();
        var accessor = FakeHttpContextAccessor.Anonymous();
        var service = new AuditService(context, accessor);

        await service.LogAsync("Création", "Client", 1, "Acme");

        Assert.Equal("Système", context.AuditLogs.Single().UserName);
    }

    [Fact]
    public async Task LogAsync_TruncatesDetails_WhenExceedingMaxLength()
    {
        using var context = TestDbContextFactory.Create();
        var accessor = FakeHttpContextAccessor.Authenticated("admin@minicrm.dev");
        var service = new AuditService(context, accessor);
        var longDetails = new string('x', 600);

        await service.LogAsync("Modification", "Client", 1, "Acme", longDetails);

        var log = context.AuditLogs.Single();
        Assert.True(log.Details!.Length <= 500);
        Assert.EndsWith("…", log.Details);
    }

    [Fact]
    public async Task GetPagedAsync_ReturnsEntriesOrderedByMostRecentFirst()
    {
        using var context = TestDbContextFactory.Create();
        var accessor = FakeHttpContextAccessor.Authenticated("admin@minicrm.dev");
        var service = new AuditService(context, accessor);
        await service.LogAsync("Création", "Client", 1, "Premier");
        await Task.Delay(10);
        await service.LogAsync("Création", "Client", 2, "Second");

        var result = await service.GetPagedAsync(page: 1, pageSize: 10);

        Assert.Equal(2, result.TotalCount);
        Assert.Equal("Second", result.Items[0].EntityLabel);
        Assert.Equal("Premier", result.Items[1].EntityLabel);
    }

    [Fact]
    public async Task GetPagedAsync_RespectsPagination()
    {
        using var context = TestDbContextFactory.Create();
        var accessor = FakeHttpContextAccessor.Authenticated("admin@minicrm.dev");
        var service = new AuditService(context, accessor);
        for (var i = 0; i < 5; i++)
            await service.LogAsync("Création", "Client", i, $"Client {i}");

        var page1 = await service.GetPagedAsync(page: 1, pageSize: 2);

        Assert.Equal(5, page1.TotalCount);
        Assert.Equal(2, page1.Items.Count);
        Assert.Equal(3, page1.TotalPages);
    }
}
