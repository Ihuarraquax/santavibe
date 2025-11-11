namespace SantaVibe.Api.Features.Profile.GetProfile;

/// <summary>
/// Response model for GET /api/profile
/// Contains user profile information
/// </summary>
public class GetProfileResponse
{
    /// <summary>
    /// Unique user identifier
    /// </summary>
    public required string UserId { get; set; }

    /// <summary>
    /// User's email address (read-only)
    /// </summary>
    public required string Email { get; set; }

    /// <summary>
    /// User's first name
    /// </summary>
    public required string FirstName { get; set; }

    /// <summary>
    /// User's last name
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
