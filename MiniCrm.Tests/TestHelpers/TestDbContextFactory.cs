using Microsoft.EntityFrameworkCore;
using MiniCrm.Data;

namespace MiniCrm.Tests.TestHelpers;

public static class TestDbContextFactory
{
    // Chaque appel crée une base InMemory isolée (nom unique), sans intercepteur.
    public static ApplicationDbContext Create()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    // Variante avec l'intercepteur d'audit branché (pour tester la journalisation automatique).
    public static ApplicationDbContext CreateWithInterceptor(AuditSaveChangesInterceptor interceptor)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .AddInterceptors(interceptor)
            .Options;

        return new ApplicationDbContext(options);
    }
}
