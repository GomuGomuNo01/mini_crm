using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Rendering;
using MiniCrm.Models;

namespace MiniCrm.Helpers;

// Langage visuel partagé par toutes les vues : libellés français, tons et
// icônes des statuts, monogrammes, montants, durée de vie des contrats et
// lecture du journal d'audit. Aucune vue ne réinvente ces règles.
public static partial class Ui
{
    private static readonly CultureInfo Fr = CultureInfo.GetCultureInfo("fr-FR");

    // Libellé + ton (success, warning, danger, neutral, info, alert) + icône Bootstrap Icons.
    public readonly record struct Look(string Label, string Tone, string Icon);

    public static Look Of(ContractStatus status) => status switch
    {
        ContractStatus.Active => new("Actif", "success", "bi-check2-circle"),
        ContractStatus.Expired => new("Expiré", "neutral", "bi-hourglass-bottom"),
        ContractStatus.Cancelled => new("Annulé", "danger", "bi-x-circle"),
        ContractStatus.Draft => new("Brouillon", "warning", "bi-pencil"),
        _ => new(status.ToString(), "neutral", "bi-circle")
    };

    public static Look Of(ClientStatus status) => status switch
    {
        ClientStatus.Active => new("Actif", "success", "bi-check2-circle"),
        ClientStatus.Inactive => new("Inactif", "neutral", "bi-pause-circle"),
        ClientStatus.Prospect => new("Prospect", "info", "bi-stars"),
        _ => new(status.ToString(), "neutral", "bi-circle")
    };

    // Clé = valeur brute de l'enum (ex. "Active"), utilisée par le dashboard.
    public static Look OfContractKey(string key) =>
        Enum.TryParse<ContractStatus>(key, out var s) ? Of(s) : new(key, "neutral", "bi-circle");

    public static IEnumerable<SelectListItem> ContractStatusOptions(ContractStatus? selected = null) =>
        Enum.GetValues<ContractStatus>().Select(s => new SelectListItem(Of(s).Label, s.ToString(), s == selected));

    public static IEnumerable<SelectListItem> ClientStatusOptions(ClientStatus? selected = null) =>
        Enum.GetValues<ClientStatus>().Select(s => new SelectListItem(Of(s).Label, s.ToString(), s == selected));

    // « Boulangerie Martin » → « BM » ; « admin@minicrm.dev » → « AD ».
    public static string Initials(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "?";
        var clean = name.Split('@')[0];
        var words = clean
            .Split(new[] { ' ', '-', '_', '.', '&' }, StringSplitOptions.RemoveEmptyEntries)
            .Where(w => char.IsLetterOrDigit(w[0]))
            .ToArray();
        if (words.Length == 0) return clean[..1].ToUpperInvariant();
        if (words.Length == 1) return words[0][..Math.Min(2, words[0].Length)].ToUpperInvariant();
        return string.Concat(char.ToUpperInvariant(words[0][0]), char.ToUpperInvariant(words[1][0]));
    }

    // Teinte stable (0-5) dérivée du texte : le même client garde toujours sa couleur.
    // (string.GetHashCode est aléatoire d'un processus à l'autre, d'où ce calcul maison.)
    public static int Hue(string? key)
    {
        if (string.IsNullOrEmpty(key)) return 0;
        var h = 7;
        foreach (var ch in key.ToLowerInvariant()) h = unchecked(h * 31 + ch);
        return (int)((uint)h % 6);
    }

    // 12000 → « 12 000 € » ; 2400,5 → « 2 400,50 € ».
    public static string Money(decimal amount) =>
        (amount % 1 == 0 ? amount.ToString("N0", Fr) : amount.ToString("N2", Fr)) + " €";

    public static string Date(DateTime date) => date.ToString("d MMM yyyy", Fr);

    public static string Plural(int count, string singular, string plural) =>
        $"{count.ToString("N0", Fr)} {(count > 1 ? plural : singular)}";

    // ----------------------------------------------------------------------
    // Durée de vie d'un contrat : où en est-on entre la date de début et la
    // date de fin ? Alimente la barre « lifespan » et sa légende.
    // ----------------------------------------------------------------------
    public readonly record struct Lifespan(double Percent, string Tone, string Caption, bool ShowNow, bool IsOverdue);

    public static Lifespan LifespanOf(Contract c, DateTime? today = null)
    {
        var day = (today ?? DateTime.Today).Date;
        var start = c.StartDate.Date;
        var end = c.EndDate.Date;

        var total = Math.Max(1, (end - start).TotalDays);
        var percent = Math.Clamp((day - start).TotalDays / total * 100, 0, 100);
        var daysLeft = (end - day).Days;

        switch (c.Status)
        {
            case ContractStatus.Draft:
                return new(0, "warning", "En attente de signature", false, false);
            case ContractStatus.Cancelled:
                return new(percent, "neutral", "Annulé", false, false);
            case ContractStatus.Expired:
                return new(100, "neutral", $"Terminé le {Date(end)}", false, false);
        }

        // Actif
        if (end < day)
            return new(100, "danger", $"Échéance dépassée de {Days(-daysLeft)}", false, true);
        if (start > day)
            return new(0, "info", $"Débute dans {Days((start - day).Days)}", false, false);
        if (daysLeft <= 30)
            return new(percent, "alert", daysLeft == 0 ? "Expire aujourd'hui" : $"Expire dans {Days(daysLeft)}", true, false);

        var months = (int)Math.Round(daysLeft / 30.44);
        var caption = daysLeft > 60
            ? $"{months} mois restant{(months > 1 ? "s" : "")}"
            : $"{Days(daysLeft)} restants";
        return new(percent, "success", caption, true, false);
    }

    public static int DaysUntil(DateTime date, DateTime? today = null) =>
        (date.Date - (today ?? DateTime.Today).Date).Days;

    private static string Days(int n) => n > 1 ? $"{n} jours" : $"{n} jour";

    // ----------------------------------------------------------------------
    // Journal d'audit
    // ----------------------------------------------------------------------
    public static Look OfAction(string action) => action switch
    {
        "Création" => new("a créé", "success", "bi-plus-lg"),
        "Modification" => new("a modifié", "info", "bi-pencil"),
        "Suppression" => new("a supprimé", "danger", "bi-trash3"),
        _ => new(action.ToLowerInvariant(), "neutral", "bi-dot")
    };

    public static string EntityPhrase(string entityType) => entityType switch
    {
        "Client" => "le client",
        "Contrat" => "le contrat",
        "Secteur" => "le secteur",
        "Utilisateur" => "l'utilisateur",
        _ => entityType.ToLowerInvariant()
    };

    // Une ligne de détail : « Statut : « Prospect » → « Active » ».
    public readonly record struct AuditChange(string? Field, string? Old, string? New, string Raw)
    {
        public bool IsDiff => Field is not null;
    }

    [GeneratedRegex(@"^(?<f>.+?) : « (?<o>.*) » → « (?<n>.*) »$")]
    private static partial Regex ChangeLine();

    public static IReadOnlyList<AuditChange> ParseDetails(string? details)
    {
        if (string.IsNullOrWhiteSpace(details)) return Array.Empty<AuditChange>();

        return details
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(line =>
            {
                var m = ChangeLine().Match(line);
                if (!m.Success) return new AuditChange(null, null, null, line);
                var field = m.Groups["f"].Value;
                return new AuditChange(field, AuditValue(field, m.Groups["o"].Value), AuditValue(field, m.Groups["n"].Value), line);
            })
            .ToList();
    }

    // Le journal stocke les valeurs brutes des enums (« Active ») : on les affiche en français.
    private static string AuditValue(string field, string value)
    {
        if (field != "Statut") return value;
        if (Enum.TryParse<ContractStatus>(value, out var contract)) return Of(contract).Label;
        if (Enum.TryParse<ClientStatus>(value, out var client)) return Of(client).Label;
        return value;
    }

    // « Aujourd'hui », « Hier », « Lundi 6 octobre », « Mardi 3 juin 2025 ».
    public static string DayLabel(DateTime localDate, DateTime? today = null)
    {
        var t = (today ?? DateTime.Today).Date;
        var d = localDate.Date;
        if (d == t) return "Aujourd'hui";
        if (d == t.AddDays(-1)) return "Hier";
        var s = d.ToString(d.Year == t.Year ? "dddd d MMMM" : "dddd d MMMM yyyy", Fr);
        return char.ToUpper(s[0], Fr) + s[1..];
    }

    public static string Greeting(DateTime? now = null) =>
        (now ?? DateTime.Now).Hour is >= 18 or < 5 ? "Bonsoir" : "Bonjour";
}
