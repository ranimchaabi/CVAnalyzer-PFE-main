// ViewModels/CreateUserViewModel.cs
using System.ComponentModel.DataAnnotations;

namespace Administration.ViewModels
{
    public class CreateUserViewModel
    {
        [Required]
        public string NomUtilisateur { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        public string MotPasse { get; set; } = string.Empty;

        [Required]
        public string Role { get; set; } = string.Empty;

        // Nouveau : rempli seulement si Role == "Directeur"
        public string? Departements { get; set; }
    }
}