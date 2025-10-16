using System.ComponentModel.DataAnnotations;

namespace SantaVibe.Api.Validators;

/// <summary>
/// Validates password complexity requirements
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class PasswordComplexityAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(
        object? value,
        ValidationContext validationContext)
    {
        if (value is not string password)
            return new ValidationResult("Password is required");

        var errors = new List<string>();

        if (!password.Any(char.IsUpper))
            errors.Add("Must contain at least one uppercase letter");

        if (!password.Any(char.IsLower))
            errors.Add("Must contain at least one lowercase letter");

        if (!password.Any(char.IsDigit))
            errors.Add("Must contain at least one digit");

        if (!password.Any(ch => !char.IsLetterOrDigit(ch)))
            errors.Add("Must contain at least one special character");

        return errors.Any()
            ? new ValidationResult(string.Join("; ", errors))
            : ValidationResult.Success;
    }
}
