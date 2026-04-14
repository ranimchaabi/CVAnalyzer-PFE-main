using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CvParsing.Models;

[Table("DonneesCvs")]
public class DonneesCv
{
    [Key]
    public int Id { get; set; }

    public int CvId { get; set; }

    public string? NomCandidat { get; set; }
    public string? Email { get; set; }
    public string? Telephone { get; set; }

    [ForeignKey(nameof(CvId))]
    public virtual Cv Cv { get; set; } = null!;
}
