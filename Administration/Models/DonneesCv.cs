namespace Administration.Models
{
    public class DonneesCv
    {
        public int Id { get; set; }
        public int CvId { get; set; }

        public string? NomCandidat { get; set; }
        public string? Email { get; set; }
        public string? Telephone { get; set; }

        public virtual Cv Cv { get; set; } = null!;
    }
}