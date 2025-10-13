using System.ComponentModel.DataAnnotations;

namespace SantaVibe.Api.DTOs;

/// <summary>
/// Request DTO for creating a new Secret Santa group
/// </summary>
public class CreateGroupRequest
{
    [Required(ErrorMessage = "Group name is required")]
    [StringLength(200, MinimumLength = 1, ErrorMessage = "Group name must be between 1 and 200 characters")]
    public required string Name { get; set; }
}
