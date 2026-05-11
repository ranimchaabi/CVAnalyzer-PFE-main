using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Administration.Models
{
    public class Cv
    {
        [Key]
        public int Id { get; set; }

        public int OffreId { get; set; }
        public int UtilisateurId { get; set; }

        public string CheminFichier { get; set; } = string.Empty;
        public DateTime UploadDate { get; set; } = DateTime.Now;
        public string ValidationStatus { get; set; } = "Pending";

        public string? NomCandidat { get; set; }
        public string? Email { get; set; }
        public string? Telephone { get; set; }
        public string? Competences { get; set; }
        public string? Experience { get; set; }
        public string? NiveauEducation { get; set; }
        public string? AutresInfos { get; set; }

        public virtual OffreEmploi Offre { get; set; } = null!;
        public virtual Utilisateur Utilisateur { get; set; } = null!;

        public virtual ICollection<Match> Matches { get; set; }
            = new List<Match>();
    }
}