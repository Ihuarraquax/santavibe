using System.ComponentModel.DataAnnotations;

namespace SantaVibe.Api.Features.Authentication.Register;

/// <summary>
/// Request model for user registration
/// </summary>
public class RegisterRequest
{
    /// <summary>
    /// User's email address (used for login)
    /// </summary>
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    [MaxLength(256, ErrorMessage = "Email cannot exceed 256 characters")]
    public required string Email { get; set; }

    /// <summary>
    /// User's password (must meet complexity requirements)
    /// </summary>
    [Required(ErrorMessage = "Password is required")]
    [MinLength(8, ErrorMessage = "Password must be at least 8 characters")]
    [PasswordComplexity]
    public required string Password { get; set; }

    /// <summary>
    /// User's first name
    /// </summary>
    [Required(ErrorMessage = "First name is required")]
    [MaxLength(100, ErrorMessage = "First name cannot exceed 100 characters")]
    public required string FirstName { get; set; }

    /// <summary>
    /// User's last name
    /// </summary>
    [Required(ErrorMessage = "Last name is required")]
    [MaxLength(100, ErrorMessage = "Last name cannot exceed 100 characters")]
    public required string LastName { get; set; }

    /// <summary>
    /// GDPR consent flag (must be true to register)
    /// </summary>
    [MustBeTrue(ErrorMessage = "GDPR consent is required")]
    public bool GdprConsent { get; set; }
}

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
            return new ValidationResult(
                "Password is required",
                new[] { validationContext.MemberName ?? "Password" });

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
            ? new ValidationResult(
                string.Join("; ", errors),
                new[] { validationContext.MemberName ?? "Password" })
            : ValidationResult.Success;
    }
}

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
            ErrorMessage ?? "This field must be true",
            new[] { validationContext.MemberName ?? "GdprConsent" });
    }
}
