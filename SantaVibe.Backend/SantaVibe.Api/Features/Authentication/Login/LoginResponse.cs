namespace SantaVibe.Api.Features.Authentication.Login;

/// <summary>
/// Response model for successful user login
/// </summary>
public class LoginResponse
{
    /// <summary>
    /// Unique identifier for the user
    /// </summary>
    public required string UserId { get; set; }

    /// <summary>
    /// User's email address
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
    /// JWT access token for authentication
    /// </summary>
    public required string Token { get; set; }

    /// <summary>
    /// Token expiration timestamp
    /// </summary>
    public DateTimeOffset ExpiresAt { get; set; }
}
