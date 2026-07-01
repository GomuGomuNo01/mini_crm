using System.ComponentModel.DataAnnotations;

namespace MiniCrm.ViewModels;

// Ligne de la liste des utilisateurs.
public class UserListItem
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsCurrentUser { get; set; }
}

// Formulaire de création d'un utilisateur (réservé aux admins).
public class CreateUserViewModel
{
    [Required(ErrorMessage = "L'email est obligatoire.")]
    [EmailAddress(ErrorMessage = "Email invalide.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Le mot de passe est obligatoire.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Le mot de passe doit faire au moins {2} caractères.")]
    [DataType(DataType.Password)]
    [Display(Name = "Mot de passe")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Le rôle est obligatoire.")]
    [Display(Name = "Rôle")]
    public string Role { get; set; } = "User";
}
