using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MiniCrm.Models;

public class Contract
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Le titre est obligatoire.")]
    [MaxLength(150)]
    [Display(Name = "Titre")]
    public string Title { get; set; } = string.Empty;

    [Display(Name = "Montant")]
    [Range(0, double.MaxValue, ErrorMessage = "Le montant doit être positif.")]
    public decimal Amount { get; set; }

    [Display(Name = "Date de début")]
    [DataType(DataType.Date)]
    public DateTime StartDate { get; set; } = DateTime.Today;

    [Display(Name = "Date de fin")]
    [DataType(DataType.Date)]
    public DateTime EndDate { get; set; } = DateTime.Today.AddYears(1);

    [Display(Name = "Statut")]
    public ContractStatus Status { get; set; } = ContractStatus.Active;

    // FK vers Client
    [Display(Name = "Client")]
    public int ClientId { get; set; }

    public Client Client { get; set; } = null!;

    // Propriété calculée : expiration dans <= 30 jours
    [NotMapped]
    public bool IsExpiringSoon =>
        Status == ContractStatus.Active &&
        EndDate <= DateTime.UtcNow.AddDays(30) &&
        EndDate >= DateTime.UtcNow;
}

public enum ContractStatus
{
    Active,
    Expired,
    Cancelled,
    Draft
}
