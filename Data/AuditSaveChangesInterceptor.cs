using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using MiniCrm.Models;

namespace MiniCrm.Data;

// Journalise automatiquement, à chaque SaveChanges, toute création / modification /
// suppression des entités métier (Client, Contrat, Secteur). Garantit que toute
// action modifiant la base est tracée dans l'audit, sans dépendre des controllers.
public class AuditSaveChangesInterceptor : SaveChangesInterceptor
{
    private readonly IHttpContextAccessor _http;
    private readonly List<Pending> _pending = new();
    private bool _writing;

    public AuditSaveChangesInterceptor(IHttpContextAccessor http) => _http = http;

    private sealed class Pending
    {
        public EntityEntry Entry = default!;
        public string EntityType = "";
        public string Action = "";
        public List<(string Label, object? Old, object? New)> Changes = new();
        public bool ClientIdChanged;
        public int? OldClientId;
        public int? NewClientId;
    }

    private static readonly Dictionary<string, string> ClientProps = new()
        { { "Name", "Nom" }, { "Email", "Email" }, { "Sector", "Secteur" }, { "Status", "Statut" } };
    private static readonly Dictionary<string, string> ContractProps = new()
        { { "Title", "Titre" }, { "Amount", "Montant" }, { "StartDate", "Début" }, { "EndDate", "Fin" }, { "Status", "Statut" } };
    private static readonly Dictionary<string, string> SectorProps = new()
        { { "Name", "Nom" } };

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        Capture(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        Capture(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
    {
        Write(eventData.Context).GetAwaiter().GetResult();
        return base.SavedChanges(eventData, result);
    }

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData, int result, CancellationToken cancellationToken = default)
    {
        await Write(eventData.Context);
        return await base.SavedChangesAsync(eventData, result, cancellationToken);
    }

    private void Capture(DbContext? ctx)
    {
        _pending.Clear();
        if (ctx == null || _writing) return;

        // Auditer uniquement les actions d'un utilisateur authentifié (exclut le seed système).
        if (_http.HttpContext?.User?.Identity?.IsAuthenticated != true) return;

        foreach (var entry in ctx.ChangeTracker.Entries())
        {
            var type = entry.Entity switch
            {
                Client => "Client",
                Contract => "Contrat",
                Sector => "Secteur",
                _ => null
            };
            if (type == null) continue;
            if (entry.State is not (EntityState.Added or EntityState.Modified or EntityState.Deleted)) continue;

            var p = new Pending { Entry = entry, EntityType = type };

            if (entry.State == EntityState.Added) p.Action = "Création";
            else if (entry.State == EntityState.Deleted) p.Action = "Suppression";
            else
            {
                p.Action = "Modification";
                var map = type == "Client" ? ClientProps : type == "Contrat" ? ContractProps : SectorProps;

                foreach (var prop in entry.Properties)
                {
                    if (!prop.IsModified) continue;
                    var name = prop.Metadata.Name;

                    if (type == "Contrat" && name == "ClientId")
                    {
                        if (!Equals(prop.OriginalValue, prop.CurrentValue))
                        {
                            p.ClientIdChanged = true;
                            p.OldClientId = prop.OriginalValue as int?;
                            p.NewClientId = prop.CurrentValue as int?;
                        }
                        continue;
                    }

                    if (!map.TryGetValue(name, out var label)) continue;
                    if (Equals(prop.OriginalValue, prop.CurrentValue)) continue;
                    p.Changes.Add((label, prop.OriginalValue, prop.CurrentValue));
                }

                if (p.Changes.Count == 0 && !p.ClientIdChanged) continue; // aucune modif pertinente
            }

            _pending.Add(p);
        }
    }

    private async Task Write(DbContext? ctx)
    {
        if (ctx == null || _pending.Count == 0) return;

        var items = _pending.ToList();
        _pending.Clear();

        var user = _http.HttpContext?.User;
        var userName = user?.Identity?.Name ?? "Système";
        string? role = user == null ? null : user.IsInRole("Admin") ? "Admin" : user.IsInRole("User") ? "User" : null;

        foreach (var p in items)
        {
            string? label = p.EntityType switch
            {
                "Client" => (p.Entry.Entity as Client)?.Name,
                "Contrat" => (p.Entry.Entity as Contract)?.Title,
                "Secteur" => (p.Entry.Entity as Sector)?.Name,
                _ => null
            };

            int? id = p.Entry.Properties.FirstOrDefault(x => x.Metadata.Name == "Id")?.CurrentValue as int?;

            string? details = null;
            if (p.Action == "Modification")
            {
                var lines = new List<string>();
                foreach (var (lbl, ov, nv) in p.Changes)
                    lines.Add($"{lbl} : « {Fmt(ov)} » → « {Fmt(nv)} »");

                if (p.ClientIdChanged)
                {
                    var oldName = p.OldClientId.HasValue ? await NameOfClient(ctx, p.OldClientId.Value) : null;
                    var newName = p.NewClientId.HasValue ? await NameOfClient(ctx, p.NewClientId.Value) : null;
                    lines.Add($"Client : « {oldName ?? "(vide)"} » → « {newName ?? "(vide)"} »");
                }

                details = string.Join("\n", lines);
            }

            ctx.Set<AuditLog>().Add(new AuditLog
            {
                Timestamp = DateTime.UtcNow,
                UserName = userName,
                UserRole = role,
                Action = p.Action,
                EntityType = p.EntityType,
                EntityId = id,
                EntityLabel = Trunc(label, 200),
                Details = Trunc(details, 500)
            });
        }

        _writing = true;
        try { await ctx.SaveChangesAsync(); }
        finally { _writing = false; }
    }

    private static async Task<string?> NameOfClient(DbContext ctx, int id) =>
        await ctx.Set<Client>().Where(c => c.Id == id).Select(c => c.Name).FirstOrDefaultAsync();

    private static string Fmt(object? v) => v switch
    {
        null => "(vide)",
        decimal d => d.ToString("N2", CultureInfo.GetCultureInfo("fr-FR")) + " €",
        DateTime dt => dt.ToString("dd/MM/yyyy"),
        _ => string.IsNullOrEmpty(v.ToString()) ? "(vide)" : v.ToString()!
    };

    private static string? Trunc(string? s, int max) =>
        s != null && s.Length > max ? s[..(max - 1)] + "…" : s;
}
