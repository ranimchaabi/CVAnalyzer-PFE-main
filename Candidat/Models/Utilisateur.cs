using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CvParsing.Models;

[Table("Utilisateur")]
public class Utilisateur
{
    [Key]
    public int Id { get; set; }

    public string NomUtilisateur { get; set; } = string.Empty;
    public string MotPasse { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    public DateTime DateCreation { get; set; } = DateTime.Now;
    public DateTime? DateDerniereConnexion { get; set; }

    public string Role { get; set; } = "Candidat";
    public bool IsActive { get; set; } = true;

    public string? Departements { get; set; }  // départements gérés (pour directeur)
    public string? PhotoUrl { get; set; }  // URL de la photo de profil

}