using System.ComponentModel.DataAnnotations;

namespace MiniCrm.Models;

public class Client
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Le nom est obligatoire.")]
    [MaxLength(100)]
    [Display(Name = "Nom")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "L'email est obligatoire.")]
    [EmailAddress(ErrorMessage = "Email invalide.")]
    public string Email { get; set; } = string.Empty;

    [MaxLength(50)]
    [Display(Name = "Secteur")]
    public string? Sector { get; set; }

    [Display(Name = "Statut")]
    public ClientStatus Status { get; set; } = ClientStatus.Active;

    [Display(Name = "Créé le")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Contract> Contracts { get; set; } = new List<Contract>();
}

public enum ClientStatus
{
    Active,
    Inactive,
    Prospect
}
