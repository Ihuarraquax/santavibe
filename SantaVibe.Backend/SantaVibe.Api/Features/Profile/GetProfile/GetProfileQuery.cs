namespace SantaVibe.Api.Features.Profile.GetProfile;

/// <summary>
/// Query for retrieving user profile
/// </summary>
/// <param name="UserId">The ID of the user to retrieve</param>
public record GetProfileQuery(Guid UserId);
