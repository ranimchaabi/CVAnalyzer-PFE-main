using System.Text.RegularExpressions;

namespace Administration.Helpers;

/// <summary>Shared rules for user-chosen passwords (letters + digits, minimum length).</summary>
public static class PasswordRules
{
    public const int MinLength = 8;

    public static bool TryValidate(string password, out string? errorMessage)
    {
        errorMessage = null;
        if (string.IsNullOrEmpty(password))
        {
            errorMessage = $"Le mot de passe doit contenir au moins {MinLength} caractères, des lettres et des chiffres.";
            return false;
        }

        if (password.Length < MinLength)
        {
            errorMessage = $"Le mot de passe doit contenir au moins {MinLength} caractères.";
            return false;
        }

        if (!Regex.IsMatch(password, @"[A-Za-z]"))
        {
            errorMessage = "Le mot de passe doit contenir au moins une lettre (A-Z, a-z).";
            return false;
        }

        if (!Regex.IsMatch(password, @"[0-9]"))
        {
            errorMessage = "Le mot de passe doit contenir au moins un chiffre (0-9).";
            return false;
        }

        return true;
    }
}
