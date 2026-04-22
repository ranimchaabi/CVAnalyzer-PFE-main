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

        var matches = context.Matches.Where(m => m.OffreId == offreId).ToList();
        if (matches.Count > 0)
            context.Matches.RemoveRange(matches);

        var cvIds = context.Cvs.AsNoTracking().Where(c => c.OffreId == offreId).Select(c => c.Id).ToList();
        if (cvIds.Count > 0)
        {
            var comps = context.CvCompetences.Where(cc => cvIds.Contains(cc.CvId)).ToList();
            if (comps.Count > 0)
                context.CvCompetences.RemoveRange(comps);

            var donnees = context.DonneesCvs.Where(d => cvIds.Contains(d.CvId)).ToList();
            if (donnees.Count > 0)
                context.DonneesCvs.RemoveRange(donnees);

            var cvs = context.Cvs.Where(c => cvIds.Contains(c.Id)).ToList();
            context.Cvs.RemoveRange(cvs);
        }

        context.OffresEmploi.Remove(offre);
        context.SaveChanges();
        return true;
    }
}
