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
    public int Experience { get; set; }
    public string NiveauEducation { get; set; } = string.Empty;
    public string Statut { get; set; } = "ACTIF";
    public DateTime DateCreation { get; set; } = DateTime.Now;
    public int IdResponsable { get; set; }

    public int? ResponsableRHId { get; set; }
    public virtual Utilisateur? ResponsableRH { get; set; }

    public virtual ICollection<Cv> Cvs { get; set; } = new List<Cv>();
}