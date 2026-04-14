using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CvParsing.Models;

[Table("Competences")]
public class Competence
{
    [Key]
    public int Id { get; set; }

    public string Nom { get; set; } = string.Empty;
}
