using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CvParsing.Models;

[Table("CvStructuredData")]
public class CvStructuredData
{
    [Key]
    public int Id { get; set; }

    public int CvId { get; set; }

    public string Competences { get; set; } = string.Empty;
    public string Experiences { get; set; } = string.Empty;
    public string Diplomes { get; set; } = string.Empty;

    public virtual Cv Cv { get; set; } = null!;
}
