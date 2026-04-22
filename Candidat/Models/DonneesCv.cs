using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CvParsing.Models;

[Table("DonneesCvs")]
public class DonneesCv
{
    [Key]
    public int Id { get; set; }

    public int CvId { get; set; }

    [Required]
    public string NomCandidat { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    public string? Telephone { get; set; }

    public string? Competences { get; set; }

    public string? Experience { get; set; }

    public string? NiveauEducation { get; set; }

    public string? AutresInfos { get; set; }

    [ForeignKey(nameof(CvId))]
    public virtual Cv Cv { get; set; } = null!;
}
