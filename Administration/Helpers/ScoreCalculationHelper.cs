using Administration.Models;
using System.Text.RegularExpressions;

namespace Administration.Helpers
{
    public static class ScoreCalculationHelper
    {
        public static (float diploma, float experience, float skills, float global) CalculateScores(Cv cvData, OffreEmploi? offre)
        {
            if (offre == null)
            {
                return (0f, 0f, 0f, 0f);
            }

            // Calculate individual scores
            var candidateEducationRank = EducationRank(cvData.NiveauEducation);
            var requiredEducationRank = EducationRank(offre.NiveauEducation);
            var diplomaScore = requiredEducationRank <= 0
                ? 100f
                : Clamp((candidateEducationRank / (float)requiredEducationRank) * 100f);

            var candidateYears = ExtractYears(cvData.Experience);
            var requiredYears = Math.Max(0, offre.Experience);
            var yearsScore = requiredYears <= 0
                ? 100f
                : Clamp((candidateYears / (float)requiredYears) * 100f);

            var candidateSkills = ParseSkillSet(cvData.Competences);
            var requiredSkills = ParseSkillSet(offre.CompetencesRequises);

            float skillsScore;
            if (requiredSkills.Count == 0)
            {
                skillsScore = candidateSkills.Count > 0 ? 100f : 50f;
            }
            else
            {
                var matched = requiredSkills.Count(req => candidateSkills.Any(cs => AreSkillsEquivalent(cs, req)));
                skillsScore = Clamp((matched / (float)requiredSkills.Count) * 100f);
            }

            var experienceRelevanceScore = ExperienceRelevance(cvData.Experience, requiredSkills, requiredYears);
            var experienceScore = Clamp((yearsScore + experienceRelevanceScore) / 2f);

            // Weighted global score: Skills 50%, Experience 30%, Diploma 20%
            var globalScore = Clamp((skillsScore * 0.50f) + (experienceScore * 0.30f) + (diplomaScore * 0.20f));

            return (diplomaScore, experienceScore, skillsScore, globalScore);
        }

        private static int EducationRank(string? education)
        {
            var value = (education ?? string.Empty).Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(value)) return 0;
            if (value.Contains("doctor") || value.Contains("phd")) return 5;
            if (value.Contains("ing") || value.Contains("engineer")) return 4;
            if (value.Contains("master") || value.Contains("bac+5")) return 3;
            if (value.Contains("licence") || value.Contains("bachelor") || value.Contains("bac+3")) return 2;
            if (value.Contains("bac")) return 1;
            return 0;
        }

        private static int ExtractYears(string? experience)
        {
            if (string.IsNullOrWhiteSpace(experience)) return 0;
            var match = Regex.Match(experience, @"(\d+)");
            return match.Success && int.TryParse(match.Groups[1].Value, out var years) ? years : 0;
        }

        private static float ExperienceTextCompatibility(string? cvExperience, string? offerDescription)
        {
            if (string.IsNullOrWhiteSpace(offerDescription)) return 100f;

            var requiredKeywords = ParseExperienceKeywords(offerDescription);
            if (requiredKeywords.Count == 0) return 100f;

            var candidateKeywords = ParseExperienceKeywords(cvExperience);
            var matched = candidateKeywords.Intersect(requiredKeywords).Count();
            return Clamp((matched / (float)requiredKeywords.Count) * 100f);
        }

        private static float ExperienceRelevance(string? cvExperience, HashSet<string> requiredSkills, int requiredYears)
        {
            if (requiredSkills.Count == 0 && requiredYears <= 0)
            {
                return 100f;
            }

            var experienceKeywords = ParseExperienceKeywords(cvExperience);
            if (experienceKeywords.Count == 0)
            {
                return requiredYears <= 0 ? 100f : Clamp((ExtractYears(cvExperience) >= requiredYears ? 100f : 50f));
            }

            var matched = requiredSkills.Count(req => experienceKeywords.Any(ek => AreSkillsEquivalent(ek, req)));
            var skillOverlapScore = requiredSkills.Count == 0 ? 100f : Clamp((matched / (float)requiredSkills.Count) * 100f);
            var yearScore = requiredYears <= 0 ? 100f : Clamp((ExtractYears(cvExperience) / (float)requiredYears) * 100f);
            return Clamp((skillOverlapScore * 0.65f) + (yearScore * 0.35f));
        }

        private static HashSet<string> ParseExperienceKeywords(string? text)
        {
            if (string.IsNullOrWhiteSpace(text)) return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var separators = new[] { ',', ';', '|', '/', '\n', '.', ':', '?', '!', '(', ')', '[', ']', '"', '\'', '-' };
            return text.Split(separators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(s => NormalizeSkill(s))
                .Where(s => s.Length > 2)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
        }

        private static HashSet<string> ParseSkillSet(string? text)
        {
            if (string.IsNullOrWhiteSpace(text)) return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var separators = new[] { ',', ';', '|', '/', '\n', '.', ':', '?', '!', '(', ')', '[', ']', '"', '\'', '-' };
            return text.Split(separators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(s => NormalizeSkill(s))
                .Where(s => s.Length > 1)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
        }

        private static string NormalizeSkill(string skill)
        {
            if (string.IsNullOrWhiteSpace(skill)) return string.Empty;
            var normalized = skill.Trim().ToLowerInvariant();
            normalized = Regex.Replace(normalized, "[\u0300-\u036f]", string.Empty);
            normalized = Regex.Replace(normalized, "[^a-z0-9 ]", " ");
            normalized = Regex.Replace(normalized, "\\s+", " ").Trim();

            return SkillSynonyms.TryGetValue(normalized, out var canonical)
                ? canonical
                : normalized;
        }

        private static bool AreSkillsEquivalent(string a, string b)
        {
            if (string.IsNullOrWhiteSpace(a) || string.IsNullOrWhiteSpace(b)) return false;
            if (a.Equals(b, StringComparison.OrdinalIgnoreCase)) return true;
            if (a.Contains(b, StringComparison.OrdinalIgnoreCase) || b.Contains(a, StringComparison.OrdinalIgnoreCase)) return true;
            var distance = LevenshteinDistance(a, b, 2);
            return distance <= 2;
        }

        private static int LevenshteinDistance(string a, string b, int max)
        {
            if (string.IsNullOrEmpty(a)) return b.Length;
            if (string.IsNullOrEmpty(b)) return a.Length;
            if (Math.Abs(a.Length - b.Length) > max) return max + 1;

            var prev = new int[b.Length + 1];
            var curr = new int[b.Length + 1];

            for (var j = 0; j <= b.Length; j++) prev[j] = j;

            for (var i = 1; i <= a.Length; i++)
            {
                curr[0] = i;
                var best = curr[0];
                var ca = a[i - 1];

                for (var j = 1; j <= b.Length; j++)
                {
                    var cost = ca == b[j - 1] ? 0 : 1;
                    var value = Math.Min(
                        Math.Min(curr[j - 1] + 1, prev[j] + 1),
                        prev[j - 1] + cost
                    );

                    curr[j] = value;
                    if (value < best) best = value;
                }

                if (best > max) return max + 1;
                (prev, curr) = (curr, prev);
            }

            return prev[b.Length];
        }

        private static readonly Dictionary<string, string> SkillSynonyms = new(StringComparer.OrdinalIgnoreCase)
        {
            { "gestion comptable", "comptabilite" },
            { "comptabilite", "comptabilite" },
            { "reporting financier", "comptabilite" },
            { "communication digitale", "marketing digital" },
            { "marketing digital", "marketing digital" },
            { "gestion de projet", "gestion de projet" },
            { "management de projet", "gestion de projet" },
            { "chef de projet", "gestion de projet" },
            { "service client", "relation client" },
            { "relation client", "relation client" },
            { "excel", "tableur" },
            { "tableur", "tableur" },
            { "power bi", "tableau de bord" },
            { "tableau de bord", "tableau de bord" },
            { "data analysis", "analyse de donnees" },
            { "analyse de donnees", "analyse de donnees" }
        };

        private static float Clamp(float value) => MathF.Max(0f, MathF.Min(100f, value));
    }
}
