using Administration.Data;
using Microsoft.EntityFrameworkCore;

namespace Administration.Helpers;

/// <summary>Deletes an offer and related rows in an order compatible with FK constraints (Match.OffreId uses NoAction).</summary>
public static class OffreDeletionHelper
{
    public static bool TryDeleteOffre(ApplicationDbContext context, int offreId, out string? errorMessage)
    {
        errorMessage = null;
        var offre = context.OffresEmploi.FirstOrDefault(o => o.Id == offreId);
        if (offre == null)
        {
            errorMessage = "Poste non trouvé.";
            return false;
        }

<<<<<<< HEAD
        var cvIds = context.Cvs.AsNoTracking().Where(c => c.OffreId == offreId).Select(c => c.Id).ToList();

        // Supprimer les matches liés aux CVs (par CvId)
        if (cvIds.Count > 0)
        {
            var matchesByCv = context.Matches.Where(m => cvIds.Contains(m.CvId)).ToList();
            if (matchesByCv.Count > 0)
                context.Matches.RemoveRange(matchesByCv);
        }

        // Supprimer les matches liés directement à l'offre (par OffreId)
        var matchesByOffre = context.Matches.Where(m => m.OffreId == offreId).ToList();
        if (matchesByOffre.Count > 0)
            context.Matches.RemoveRange(matchesByOffre);

        // Supprimer les CVs liés
=======
        var matches = context.Matches.Where(m => m.OffreId == offreId).ToList();
        if (matches.Count > 0)
            context.Matches.RemoveRange(matches);

        var cvIds = context.Cvs.AsNoTracking().Where(c => c.OffreId == offreId).Select(c => c.Id).ToList();
>>>>>>> 364b0a3c7128899c3df393e4339086a493d07ccc
        if (cvIds.Count > 0)
        {
            var cvs = context.Cvs.Where(c => cvIds.Contains(c.Id)).ToList();
            context.Cvs.RemoveRange(cvs);
        }

        context.OffresEmploi.Remove(offre);
        context.SaveChanges();
        return true;
    }
<<<<<<< HEAD
}
=======
}
>>>>>>> 364b0a3c7128899c3df393e4339086a493d07ccc
