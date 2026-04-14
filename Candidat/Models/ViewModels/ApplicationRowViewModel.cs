namespace CvParsing.Models.ViewModels;

public class ApplicationRowViewModel
{
    public int CvId { get; set; }
    public int OffreId { get; set; }
    public string TitrePoste { get; set; } = string.Empty;
    public string DepartementOuEntreprise { get; set; } = string.Empty;
    public DateTime DateCandidature { get; set; }
    public string Statut { get; set; } = "Pending";
}
