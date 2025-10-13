using System.ComponentModel.DataAnnotations;

namespace SantaVibe.Api.Models;

public class CreateGroupRequest
{
    [Required(ErrorMessage = "Name is required")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "Name must be between 3 and 100 characters")]
    public required string Name { get; set; }

    [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
    public string? Description { get; set; }

    [Range(1, 100000, ErrorMessage = "Budget must be between 1 and 100,000 PLN")]
    public decimal? Budget { get; set; }

    [DataType(DataType.DateTime)]
    public DateTime? DrawDate { get; set; }
}
