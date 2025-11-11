namespace SantaVibe.Api.Features.Profile.UpdateProfile;

/// <summary>
/// Command for updating user profile
/// </summary>
/// <param name="UserId">The ID of the user to update</param>
/// <param name="FirstName">The updated first name</param>
/// <param name="LastName">The updated last name</param>
public record UpdateProfileCommand(
    Guid UserId,
    string FirstName,
    string LastName
);
