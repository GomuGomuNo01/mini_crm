using MiniCrm.Models;

namespace MiniCrm.ViewModels;

public class DashboardViewModel
{
    public int TotalActiveClients { get; set; }
    public int ProspectClients { get; set; }
    public int TotalActiveContracts { get; set; }
    public int ContractsExpiringSoon { get; set; }
    public decimal TotalContractValue { get; set; }

    // Contrats toujours « Actifs » dont la date de fin est passée : à renouveler ou clôturer.
    public int OverdueActiveContracts { get; set; }

    public List<Contract> ExpiringContracts { get; set; } = new();
    public List<Contract> RecentContracts { get; set; } = new();
    public Dictionary<string, int> ContractsByStatus { get; set; } = new();
}
