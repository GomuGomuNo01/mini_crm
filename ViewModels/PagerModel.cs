namespace MiniCrm.ViewModels;

// Données du composant de pagination partagé (Views/Shared/_Pager.cshtml).
// UrlFor construit le lien d'une page en conservant les filtres courants.
public record PagerModel(int Page, int TotalPages, Func<int, string> UrlFor)
{
    // Fenêtre de pages affichées : 1 … 4 5 6 … 12 (null = ellipse).
    public IEnumerable<int?> Window()
    {
        var pages = new SortedSet<int> { 1, TotalPages, Page - 1, Page, Page + 1 };
        int? previous = null;
        foreach (var p in pages.Where(p => p >= 1 && p <= TotalPages))
        {
            if (previous is not null && p - previous > 1) yield return null;
            yield return p;
            previous = p;
        }
    }
}
