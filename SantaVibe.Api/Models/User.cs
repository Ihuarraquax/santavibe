namespace SantaVibe.Api.Models;

/// <summary>
/// Represents an application user
/// </summary>
public class User
{
    public Guid Id { get; set; }

    public required string Email { get; set; }

    public required string FirstName { get; set; }

    public required string LastName { get; set; }

    public required string PasswordHash { get; set; }

    public string? Wishlist { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? WishlistUpdatedAt { get; set; }

    public ICollection<Group> OrganizedGroups { get; set; } = new List<Group>();

    public ICollection<Participant> Participations { get; set; } = new List<Participant>();
}
