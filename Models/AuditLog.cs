using System.ComponentModel.DataAnnotations;

namespace MiniCrm.Models;

// Journal d'audit : trace les actions ayant un impact sur les données
// (création / modification / suppression) avec l'auteur et l'horodatage.
public class AuditLog
{
    public int Id { get; set; }

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    [MaxLength(256)]
    public string UserName { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? UserRole { get; set; }

    // Création, Modification, Suppression
    [MaxLength(50)]
    public string Action { get; set; } = string.Empty;

    // Client, Contrat
    [MaxLength(50)]
    public string EntityType { get; set; } = string.Empty;

    public int? EntityId { get; set; }

    [MaxLength(200)]
    public string? EntityLabel { get; set; }

    [MaxLength(500)]
    public string? Details { get; set; }
}
