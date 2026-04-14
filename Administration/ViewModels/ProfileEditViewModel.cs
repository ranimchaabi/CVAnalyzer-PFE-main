namespace Administration.ViewModels
{
    public class ProfileEditViewModel
    {
        public int Id { get; set; }
        public string NomUtilisateur { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        
        public string Bio { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
       
        public string NewPassword { get; set; } = string.Empty;
    }
}