using System.ComponentModel.DataAnnotations;
using SantaVibe.Api.Validators;

namespace SantaVibe.Api.DTOs.Auth;

/// <summary>
/// Request model for user registration
/// </summary>
public class RegisterUserRequest
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
