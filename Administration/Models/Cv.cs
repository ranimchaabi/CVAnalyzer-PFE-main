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

        public virtual OffreEmploi Offre { get; set; } = null!;
        public virtual Utilisateur Utilisateur { get; set; } = null!;

        public virtual DonneesCv? DonneesCv { get; set; }

        public virtual ICollection<Match> Matches { get; set; }
            = new List<Match>();

        public virtual ICollection<CvCompetence> CvCompetences { get; set; }
            = new List<CvCompetence>();
    }
}