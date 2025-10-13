namespace SantaVibe.Api.Models;

public class Group
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public decimal? Budget { get; set; }
    public DateTime? DrawDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public required string OrganizerId { get; set; }
    public bool IsDrawCompleted { get; set; }
    public string? InvitationCode { get; set; }
}
