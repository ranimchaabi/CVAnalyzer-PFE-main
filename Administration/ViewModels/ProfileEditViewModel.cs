using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;
using Administration.Validation;

namespace Administration.ViewModels
{
    public class ProfileEditViewModel
    {
        public int Id { get; set; }
<<<<<<< HEAD

        [Required(ErrorMessage = "Le nom d'utilisateur est requis.")]
        [Display(Name = "Nom d'utilisateur")]
        public string NomUtilisateur { get; set; } = string.Empty;

=======
        
        [Required(ErrorMessage = "Le nom d'utilisateur est requis.")]
        [Display(Name = "Nom d'utilisateur")]
        public string NomUtilisateur { get; set; } = string.Empty;
        
>>>>>>> 364b0a3c7128899c3df393e4339086a493d07ccc
        [Required(ErrorMessage = "L'email est requis.")]
        [EmailAddress(ErrorMessage = "Email invalide.")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;
<<<<<<< HEAD

        [Display(Name = "Photo de profil")]
        public IFormFile? ProfileImage { get; set; }

        [Display(Name = "Mot de passe actuel")]
        public string? CurrentPassword { get; set; }

        [Display(Name = "Nouveau mot de passe")]
        [StrongPassword]
        public string? NewPassword { get; set; }

        [Display(Name = "Confirmer le nouveau mot de passe")]
        [Compare("NewPassword", ErrorMessage = "Les mots de passe ne correspondent pas.")]
        public string? ConfirmNewPassword { get; set; }

        public string? CurrentPhotoUrl { get; set; }

        // ✅ AJOUT : indique quel onglet a été soumis
        public string ActiveTab { get; set; } = "profile";
=======
        
        [Display(Name = "Photo de profil")]
        public IFormFile? ProfileImage { get; set; }
        
        [Display(Name = "Mot de passe actuel")]
        public string? CurrentPassword { get; set; }
        
        [Display(Name = "Nouveau mot de passe")]
        [StrongPassword]
        public string? NewPassword { get; set; }
        
        [Display(Name = "Confirmer le nouveau mot de passe")]
        [Compare("NewPassword", ErrorMessage = "Les mots de passe ne correspondent pas.")]
        public string? ConfirmNewPassword { get; set; }
        
        public string? CurrentPhotoUrl { get; set; }
>>>>>>> 364b0a3c7128899c3df393e4339086a493d07ccc
    }
}