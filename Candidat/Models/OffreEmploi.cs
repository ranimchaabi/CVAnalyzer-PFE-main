<<<<<<< HEAD
using System.ComponentModel.DataAnnotations;
=======
>>>>>>> 364b0a3c7128899c3df393e4339086a493d07ccc
using System.ComponentModel.DataAnnotations.Schema;

namespace CvParsing.Models;

[Table("OffreEmploi")]
public class OffreEmploi
{
    public int Id { get; set; }
    public string Titre { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Departement { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
<<<<<<< HEAD
    [Required(ErrorMessage = "Veuillez renseigner l'expérience requise.")]
    public int Experience { get; set; }
    [Required(ErrorMessage = "Veuillez renseigner le niveau d'éducation.")]
    public string NiveauEducation { get; set; } = string.Empty;
    [Required(ErrorMessage = "Veuillez renseigner le type de diplôme.")]
    public string TypeDiplome { get; set; } = string.Empty;
    public string CompetencesRequises { get; set; } = string.Empty;
=======
    public int Experience { get; set; }
    public string NiveauEducation { get; set; } = string.Empty;
>>>>>>> 364b0a3c7128899c3df393e4339086a493d07ccc
    public string Statut { get; set; } = "ACTIF";
    public DateTime DateCreation { get; set; } = DateTime.Now;
    public int IdResponsable { get; set; }

    public int? ResponsableRHId { get; set; }
    public virtual Utilisateur? ResponsableRH { get; set; }

    public virtual ICollection<Cv> Cvs { get; set; } = new List<Cv>();
}