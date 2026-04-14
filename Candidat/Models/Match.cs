using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CvParsing.Models;

[Table("Match")]
public class Match
{
    [Key]
    public int Id { get; set; }

    public int CvId { get; set; }
    public int OffreId { get; set; }

    public float CompetenceScore { get; set; }
    public float DiplomeScore { get; set; }
    public float ExperienceScore { get; set; }
    public float GlobalScore { get; set; }

    [ForeignKey(nameof(CvId))]
    public virtual Cv Cv { get; set; } = null!;

    [ForeignKey(nameof(OffreId))]
    public virtual OffreEmploi Offre { get; set; } = null!;
}
