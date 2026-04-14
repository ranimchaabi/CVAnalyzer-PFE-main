using System.ComponentModel.DataAnnotations;

namespace Administration.ViewModels
{
    public class LoginViewModel
    {
        [Required]
        public required string NomUtilisateur { get; set; } // Ajout de 'required'

        [Required]
        [DataType(DataType.Password)]
        public required string MotPasse { get; set; } // Ajout de 'required'
    }
}