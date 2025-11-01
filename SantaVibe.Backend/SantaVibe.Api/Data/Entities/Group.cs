using SantaVibe.Api.Common;
using SantaVibe.Api.Common.DomainEvents;
using SantaVibe.Api.Data.Entities.Events;

namespace SantaVibe.Api.Data.Entities;

/// <summary>
/// Represents a Secret Santa event/group
/// </summary>
public class Group : IHasDomainEvents
{
    private readonly List<IDomainEvent> _domainEvents = new();
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public void AddDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
    /// <summary>
    /// Unique group identifier
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Group name (FR-006)
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Organizer user reference (FK to AspNetUsers)
    /// </summary>
    public required string OrganizerUserId { get; set; }

    /// <summary>
    /// Final budget in PLN (FR-011)
    /// </summary>
    public decimal? Budget { get; set; }

    /// <summary>
    /// Shareable invitation token (FR-007)
    /// </summary>
    public Guid InvitationToken { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Draw completion timestamp (FR-011)
    /// NULL = draw not performed, NOT NULL = draw completed
    /// </summary>
    public DateTimeOffset? DrawCompletedAt { get; set; }

    /// <summary>
    /// Group creation timestamp
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Last modification timestamp
    /// </summary>
    public DateTimeOffset? UpdatedAt { get; set; }

    // Navigation properties
    public ApplicationUser Organizer { get; set; } = null!;
    public ICollection<GroupParticipant> GroupParticipants { get; set; } = new List<GroupParticipant>();
    public ICollection<ExclusionRule> ExclusionRules { get; set; } = new List<ExclusionRule>();
    public ICollection<Assignment> Assignments { get; set; } = new List<Assignment>();
    public ICollection<EmailNotification> EmailNotifications { get; set; } = new List<EmailNotification>();

    // Domain logic methods

    /// <summary>
    /// Checks if the draw has been completed
    /// </summary>
    public bool IsDrawCompleted() => DrawCompletedAt.HasValue;

    /// <summary>
    /// Checks if a user is already a participant in this group
    /// </summary>
    public bool HasParticipant(string userId) =>
        GroupParticipants.Any(gp => gp.UserId == userId);

    /// <summary>
    /// Adds a new participant to the group
    /// </summary>
    /// <param name="userId">User ID to add</param>
    /// <param name="budgetSuggestion">Optional budget suggestion</param>
    /// <returns>Result containing the new participant or error information</returns>
    public Result<GroupParticipant> AddParticipant(string userId, decimal? budgetSuggestion)
    {
        // Business rule: Cannot join after draw is completed
        if (IsDrawCompleted())
        {
            return Result<GroupParticipant>.Failure(
                "InvitationExpired",
                "This group has already completed the draw and is no longer accepting participants");
        }

        // Business rule: Cannot join if already a participant
        if (HasParticipant(userId))
        {
            return Result<GroupParticipant>.Failure(
                "AlreadyParticipant",
                "You are already a participant in this group");
        }

        var participant = new GroupParticipant
        {
            GroupId = Id,
            UserId = userId,
            BudgetSuggestion = budgetSuggestion,
            JoinedAt = DateTimeOffset.UtcNow,
            WishlistContent = null,
            WishlistLastModified = null
        };

        GroupParticipants.Add(participant);

        return Result<GroupParticipant>.Success(participant);
    }

    /// <summary>
    /// Gets the total count of participants including the organizer
    /// </summary>
    public int GetParticipantCount() => GroupParticipants.Count;

    /// <summary>
    /// Executes the Secret Santa draw for this group
    /// </summary>
    /// <param name="budget">The budget for gifts</param>
    /// <param name="santaToRecipientMap">Dictionary mapping Santa user IDs to recipient user IDs</param>
    /// <returns>Result indicating success or failure</returns>
    public Result<Dictionary<string, string>> ExecuteDraw(decimal budget, Dictionary<string, string> santaToRecipientMap)
    {
        // Business rule: Cannot execute draw if already completed
        if (IsDrawCompleted())
        {
            return Result<Dictionary<string, string>>.Failure(
                "DrawAlreadyCompleted",
                "Draw has already been completed for this group");
        }

        // Business rule: Must have minimum 3 participants
        if (GroupParticipants.Count < 3)
        {
            return Result<Dictionary<string, string>>.Failure(
                "InsufficientParticipants",
                "Minimum 3 participants required for draw");
        }

        // Validate budget
        if (budget < 0.01m || budget > 99999999.99m)
        {
            return Result<Dictionary<string, string>>.Failure(
                "InvalidBudget",
                "Budget must be between 0.01 and 99999999.99");
        }

        // Update group state
        Budget = budget;
        DrawCompletedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;

        // Create assignments (part of the aggregate)
        var assignmentEntities = santaToRecipientMap.Select(pair => new Assignment
        {
            Id = Guid.NewGuid(),
            GroupId = Id,
            SantaUserId = pair.Key,
            RecipientUserId = pair.Value,
            AssignedAt = DrawCompletedAt.Value
        }).ToList();

        // Add assignments to the aggregate's collection
        foreach (var assignment in assignmentEntities)
        {
            Assignments.Add(assignment);
        }

        // Get participant IDs for the event
        var participantIds = GroupParticipants.Select(gp => gp.UserId).ToList();

        // Raise domain event (only for email notifications - not part of aggregate)
        var drawCompletedEvent = new DrawCompletedEvent(
            GroupId: Id,
            ParticipantIds: participantIds,
            Assignments: santaToRecipientMap,
            OccurredAt: DrawCompletedAt.Value);

        AddDomainEvent(drawCompletedEvent);

        return Result<Dictionary<string, string>>.Success(santaToRecipientMap);
    }
}
