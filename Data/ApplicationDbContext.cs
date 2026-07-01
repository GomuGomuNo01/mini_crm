using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MiniCrm.Models;

namespace MiniCrm.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

    public DbSet<Client> Clients => Set<Client>();
    public DbSet<Contract> Contracts => Set<Contract>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<Sector> Sectors => Set<Sector>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // MySQL : borner les colonnes string indexées (clés Identity) sous la limite
        // d'index. Avec utf8mb4 (4 octets/caractère), varchar(256) dépasse la limite ;
        // 191 est la longueur sûre classique pour utf8mb4.
        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            var indexedProps = entityType.GetProperties()
                .Where(p => p.ClrType == typeof(string))
                .Where(p =>
                    p.IsKey() ||
                    p.IsForeignKey() ||
                    entityType.GetIndexes().Any(i => i.Properties.Contains(p)));

            foreach (var prop in indexedProps)
            {
                if ((prop.GetMaxLength() ?? int.MaxValue) > 191)
                    prop.SetMaxLength(191);
            }
        }

        builder.Entity<Contract>()
            .HasOne(c => c.Client)
            .WithMany(cl => cl.Contracts)
            .HasForeignKey(c => c.ClientId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Contract>()
            .Property(c => c.Amount)
            .HasColumnType("decimal(18,2)");
    }
}
