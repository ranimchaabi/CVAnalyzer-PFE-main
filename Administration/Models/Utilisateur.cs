// Models/Utilisateur.cs
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Administration.Models
{
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

        // Nouveau : départements gérés par ce directeur (séparés par virgule)
        public string? Departements { get; set; }
    }
}