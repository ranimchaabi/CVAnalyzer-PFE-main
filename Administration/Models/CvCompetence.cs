namespace Administration.Models
{
    public class CvCompetence
    {
        public int CvId { get; set; }
        public int CompetenceId { get; set; }

        public Cv Cv { get; set; } = null!;
        public Competence Competence { get; set; } = null!;
    }
}