using System.ComponentModel.DataAnnotations;
using Administration.Helpers;

namespace Administration.Validation;

public sealed class StrongPasswordAttribute : ValidationAttribute
{
    public StrongPasswordAttribute()
        : base("Le mot de passe doit contenir au moins 8 caractères, des lettres et des chiffres.")
    {
    }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is not string s || string.IsNullOrEmpty(s))
            return ValidationResult.Success;

        return PasswordRules.TryValidate(s, out var err)
            ? ValidationResult.Success
            : new ValidationResult(err);
    }
}
