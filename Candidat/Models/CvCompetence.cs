using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CvParsing.Models;

[Table("CvCompetences")]
public class CvCompetence
{
    public int CvId { get; set; }
    public int CompetenceId { get; set; }

    [ForeignKey(nameof(CvId))]
    public virtual Cv Cv { get; set; } = null!;

    [ForeignKey(nameof(CompetenceId))]
    public virtual Competence Competence { get; set; } = null!;
}
