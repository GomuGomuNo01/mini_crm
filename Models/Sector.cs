using System.ComponentModel.DataAnnotations;

namespace MiniCrm.Models;

// Catalogue des secteurs d'activité, géré par l'admin (liste de suggestions
// proposée dans le formulaire client).
public class Sector
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Le nom du secteur est obligatoire.")]
    [MaxLength(50)]
    [Display(Name = "Nom du secteur")]
    public string Name { get; set; } = string.Empty;
}
