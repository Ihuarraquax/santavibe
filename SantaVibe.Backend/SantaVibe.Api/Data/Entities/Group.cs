using SantaVibe.Api.Common;
using SantaVibe.Api.Common.DomainEvents;
using SantaVibe.Api.Data.Entities.Events;
using SantaVibe.Api.Services;

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
    public Guid Id { get; set; } = Guid.NewGuid();

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
    /// <param name="budget">Budget for gift exchange</param>
    /// <param name="drawAlgorithmService">Service to execute draw algorithm</param>
    /// <returns>Result containing assignment details or error information</returns>
    public Result<DrawResult> ExecuteDraw(decimal budget, IDrawAlgorithmService drawAlgorithmService)
    {
        // Business rule: Cannot execute draw if already completed
        if (IsDrawCompleted())
        {
            return Result<DrawResult>.Failure(
                "DrawAlreadyCompleted",
                "The draw has already been completed for this group");
        }

        // Business rule: Minimum 3 participants required
        var participantCount = GetParticipantCount();
        if (participantCount < 3)
        {
            return Result<DrawResult>.Failure(
                "InsufficientParticipants",
                "Minimum 3 participants required for draw");
        }

        // Get participant IDs and exclusion pairs
        var participantIds = GroupParticipants.Select(gp => gp.UserId).ToList();
        var exclusionPairs = ExclusionRules
            .Select(er => (er.UserId1, er.UserId2))
            .ToList();

        // Execute draw algorithm
        Dictionary<string, string> assignmentMap;
        try
        {
            assignmentMap = drawAlgorithmService.ExecuteDrawAlgorithm(participantIds, exclusionPairs);
        }
        catch (DrawAlgorithmException ex)
        {
            return Result<DrawResult>.Failure(
                "DrawAlgorithmFailed",
                ex.Message);
        }

        // Create assignment entities
        var now = DateTimeOffset.UtcNow;
        var assignments = assignmentMap.Select(kvp => new Assignment
        {
            Id = Guid.NewGuid(),
            GroupId = Id,
            SantaUserId = kvp.Key,
            RecipientUserId = kvp.Value,
            AssignedAt = now
        }).ToList();

        // Add assignments to the group
        foreach (var assignment in assignments)
        {
            Assignments.Add(assignment);
        }

        // Update group state
        Budget = budget;
        DrawCompletedAt = now;
        UpdatedAt = now;

        // Raise domain event
        var drawCompletedEvent = new DrawCompletedEvent(
            GroupId: Id,
            ParticipantIds: participantIds,
            Assignments: assignmentMap,
            OccurredAt: now);

        AddDomainEvent(drawCompletedEvent);

        // Return result
        var result = new DrawResult(
            Assignments: assignments,
            DrawCompletedAt: now);

        return Result<DrawResult>.Success(result);
    }
}

/// <summary>
/// Result of executing a Secret Santa draw
/// </summary>
public sealed record DrawResult(
    List<Assignment> Assignments,
    DateTimeOffset DrawCompletedAt);
