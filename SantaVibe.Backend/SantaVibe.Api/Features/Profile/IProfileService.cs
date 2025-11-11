using SantaVibe.Api.Common;
using SantaVibe.Api.Features.Profile.GetProfile;
using SantaVibe.Api.Features.Profile.UpdateProfile;

namespace SantaVibe.Api.Features.Profile;

/// <summary>
/// Service interface for profile management operations
/// </summary>
public interface IProfileService
{
    /// <summary>
    /// Retrieves a user's profile by user ID
    /// </summary>
    /// <param name="userId">The unique identifier of the user</param>
    /// <returns>Result containing the user's profile or error information</returns>
    Task<Result<GetProfileResponse>> GetProfileAsync(Guid userId);

    /// <summary>
    /// Updates a user's profile information
    /// </summary>
    /// <param name="command">The command containing user ID and updated profile data</param>
    /// <returns>Result containing the updated profile or error information</returns>
    Task<Result<UpdateProfileResponse>> UpdateProfileAsync(UpdateProfileCommand command);
}
