using System.ComponentModel.DataAnnotations;

namespace CvParsing.Models.ViewModels;

public class ProfilePageViewModel
{
    public string NomComplet { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Telephone { get; set; }
    
    // Some fields were in the UI but not directly in the new database schema
    public string? Departement { get; set; }
    public string? Designation { get; set; }
    public string? Langues { get; set; }
    public string? Bio { get; set; }
    public string? PhotoUrl { get; set; }

    public List<string> DepartementOptions { get; set; } = new();
    public List<string> DesignationOptions { get; set; } = new();

    public List<ApplicationRowViewModel> Applications { get; set; } = new();
    public int ApplicationsTotal { get; set; }
    public int ApplicationsPage { get; set; }
    public int ApplicationsPageSize { get; set; }
    public string? StatusFilter { get; set; }

    public int ApplicationsTotalPages => ApplicationsPageSize > 0 
        ? (int)Math.Ceiling((double)ApplicationsTotal / ApplicationsPageSize) 
        : 1;
}
