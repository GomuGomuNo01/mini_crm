using MiniCrm.Models;

namespace MiniCrm.ViewModels;

// Vue de liste des clients (liste + barre de recherche).
public class ClientViewModel
{
    public IEnumerable<Client> Clients { get; set; } = new List<Client>();
    public string? Search { get; set; }
}
