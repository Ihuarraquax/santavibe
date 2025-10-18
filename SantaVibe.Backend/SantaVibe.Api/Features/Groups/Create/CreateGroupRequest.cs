using System.ComponentModel.DataAnnotations;

namespace SantaVibe.Api.Features.Groups.Create;

/// <summary>
/// Request to create a new Secret Santa group
/// </summary>
public record CreateGroupRequest
{
    /// <summary>
    /// Name of the Secret Santa group
    /// </summary>
    [Required(ErrorMessage = "Group name is required")]
    [MinLength(3, ErrorMessage = "Group name must be at least 3 characters")]
    [MaxLength(200, ErrorMessage = "Group name cannot exceed 200 characters")]
    public required string Name { get; init; }
}
