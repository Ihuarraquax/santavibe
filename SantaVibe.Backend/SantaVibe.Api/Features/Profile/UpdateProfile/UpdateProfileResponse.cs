namespace SantaVibe.Api.Features.Profile.UpdateProfile;

/// <summary>
/// Response model for PUT /api/profile
/// Contains updated user profile information
/// </summary>
public class UpdateProfileResponse
{
    /// <summary>
    /// Unique user identifier
    /// </summary>
    public required string UserId { get; set; }

    /// <summary>
    /// User's email address (read-only, unchanged)
    /// </summary>
    public required string Email { get; set; }

    /// <summary>
    /// User's updated first name
    /// </summary>
    public required string FirstName { get; set; }

    /// <summary>
    /// User's updated last name
    /// </summary>
    public required string LastName { get; set; }

    /// <summary>
    /// Account creation timestamp
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Last successful login timestamp
    /// </summary>
    public DateTimeOffset? LastLoginAt { get; set; }
}
