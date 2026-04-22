using Administration.Data;
using Administration.Models;
using Microsoft.EntityFrameworkCore;

namespace Administration.Helpers
{
    public static class MatchIntegrationHelper
    {
        public static async Task EnsureMatchForCvAsync(ApplicationDbContext context, int offreId, int cvId)
        {
            var existing = await context.Matches.FirstOrDefaultAsync(m => m.OffreId == offreId && m.CvId == cvId);
            if (existing != null)
            {
                return;
            }

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
