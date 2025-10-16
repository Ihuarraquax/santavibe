using System.ComponentModel.DataAnnotations;

namespace SantaVibe.Api.Validators;

/// <summary>
/// Validates that a boolean property is true (for consent checkboxes)
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class MustBeTrueAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(
        object? value,
        ValidationContext validationContext)
    {
        if (value is bool boolValue && boolValue)
            return ValidationResult.Success;

        return new ValidationResult(
            ErrorMessage ?? "This field must be true");
    }
}
