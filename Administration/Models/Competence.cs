using System.Collections.Generic;

namespace Administration.Models
{
    public class Competence
    {
        public int Id { get; set; }
        public string Nom { get; set; } = string.Empty;

        public virtual ICollection<CvCompetence> CvCompetences { get; set; }
            = new List<CvCompetence>();
    }
}