using Administration.Models;

namespace Administration.ViewModels
{
    public class PosteDetailViewModel
    {
        public OffreEmploi Offre { get; set; } = new OffreEmploi();
        public List<Utilisateur> Candidats { get; set; } = new List<Utilisateur>();
    }
}