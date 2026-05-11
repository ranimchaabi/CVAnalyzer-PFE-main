namespace Administration.Helpers;

/// <summary>
/// Candidate photos are often stored under the Candidat site wwwroot while the admin app reads the same DB.
/// When <see cref="CandidatMediaOptions.PublicBaseUrl"/> is set, relative URLs for role Candidat are shown from that origin.
/// </summary>
public static class ProfileImageUrlHelper
{
    public static string? ResolveDisplayUrl(string? photoUrl, string? role, string? candidatPublicBaseUrl)
    {
        if (string.IsNullOrWhiteSpace(photoUrl))
            return null;

        var u = photoUrl.Trim();
        if (u.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            u.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            return u;

        if (string.Equals(role, "Candidat", StringComparison.OrdinalIgnoreCase) &&
            !string.IsNullOrWhiteSpace(candidatPublicBaseUrl))
        {
            var baseUrl = candidatPublicBaseUrl.TrimEnd('/');
            return u.StartsWith('/') ? baseUrl + u : baseUrl + "/" + u;
        }

        return u;
    }
}
