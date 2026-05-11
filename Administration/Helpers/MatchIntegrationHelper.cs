using Administration.Data;
using Administration.Models;
using Microsoft.EntityFrameworkCore;

namespace Administration.Helpers
{
    public static class MatchIntegrationHelper
    {
        public static async Task EnsureMatchForCvAsync(ApplicationDbContext context, int offreId, int cvId)
        {
            if (cvId <= 0)
            {
                return;
            }

<<<<<<< HEAD
            var cv = await context.Cvs
                .Include(c => c.Offre)
                .FirstOrDefaultAsync(c => c.Id == cvId && c.OffreId == offreId);
            
            if (cv == null)
=======
            var cvExists = await context.Cvs
                .AnyAsync(c => c.Id == cvId && c.OffreId == offreId);
            if (!cvExists)
>>>>>>> 364b0a3c7128899c3df393e4339086a493d07ccc
            {
                return;
            }

            var existing = await context.Matches.FirstOrDefaultAsync(m => m.OffreId == offreId && m.CvId == cvId);
<<<<<<< HEAD
            
            // If match already exists and has scores, don't recalculate
            if (existing != null && existing.GlobalScore > 0)
=======
            if (existing != null)
>>>>>>> 364b0a3c7128899c3df393e4339086a493d07ccc
            {
                return;
            }

<<<<<<< HEAD
            // Calculate scores using the CV data and offer requirements
            var scores = ScoreCalculationHelper.CalculateScores(cv, cv.Offre);

            if (existing != null)
            {
                // Update existing match with calculated scores
                existing.CompetenceScore = scores.skills;
                existing.DiplomeScore = scores.diploma;
                existing.ExperienceScore = scores.experience;
                existing.GlobalScore = scores.global;
            }
            else
            {
                // Create new match with calculated scores
                context.Matches.Add(new Match
                {
                    OffreId = offreId,
                    CvId = cvId,
                    CompetenceScore = scores.skills,
                    DiplomeScore = scores.diploma,
                    ExperienceScore = scores.experience,
                    GlobalScore = scores.global
                });
            }
=======
            // Create a safe placeholder row when scoring is missing.
            // This prevents 404 pages and guarantees DB/UI consistency.
            context.Matches.Add(new Match
            {
                OffreId = offreId,
                CvId = cvId,
                CompetenceScore = 0f,
                DiplomeScore = 0f,
                ExperienceScore = 0f,
                GlobalScore = 0f
            });
>>>>>>> 364b0a3c7128899c3df393e4339086a493d07ccc
        }

        public static async Task EnsureMatchesForOffreAsync(ApplicationDbContext context, int offreId)
        {
            var cvIds = await context.Cvs
                .Where(c => c.OffreId == offreId)
                .Select(c => c.Id)
                .ToListAsync();

            foreach (var cvId in cvIds)
            {
                await EnsureMatchForCvAsync(context, offreId, cvId);
            }

            await context.SaveChangesAsync();
        }
    }
}
