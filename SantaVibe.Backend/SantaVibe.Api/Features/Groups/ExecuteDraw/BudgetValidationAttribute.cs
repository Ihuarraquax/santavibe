using System.ComponentModel.DataAnnotations;

namespace SantaVibe.Api.Features.Groups.ExecuteDraw;

/// <summary>
/// Custom validation attribute for budget values
/// Ensures budget is between 0.01 and 99999999.99 and has at most 2 decimal places
/// </summary>
public class BudgetValidationAttribute : ValidationAttribute
{
    private const decimal MinBudget = 0.01m;
    private const decimal MaxBudget = 99999999.99m;

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is not decimal budget)
        {
            return new ValidationResult("Budget must be a valid decimal value");
        }

        // Check range
        if (budget < MinBudget || budget > MaxBudget)
        {
            return new ValidationResult($"Budget must be between {MinBudget} and {MaxBudget}");
        }

        // Check decimal places
        var decimalPlaces = BitConverter.GetBytes(decimal.GetBits(budget)[3])[2];
        if (decimalPlaces > 2)
        {
            return new ValidationResult("Budget must have at most 2 decimal places");
        }

        return ValidationResult.Success;
    }
}
