namespace Administration.Options;

/// <summary>Configure the public base URL of the Candidat web app so admin UI can load profile images stored there.</summary>
public class CandidatMediaOptions
{
    public const string SectionName = "CandidatMedia";

    /// <summary>E.g. https://localhost:7xxx (no trailing slash). Leave empty if Candidat and Administration share the same host/static files.</summary>
    public string PublicBaseUrl { get; set; } = "";
}
