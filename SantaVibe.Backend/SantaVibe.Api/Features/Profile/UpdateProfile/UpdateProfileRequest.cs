using System.ComponentModel.DataAnnotations;

namespace SantaVibe.Api.Features.Profile.UpdateProfile;

/// <summary>
/// Request model for PUT /api/profile
/// Contains updatable user profile fields
/// </summary>
public class UpdateProfileRequest
{
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
}
