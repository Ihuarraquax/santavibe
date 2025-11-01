namespace SantaVibe.Api.Features.Groups.ExecuteDraw;

/// <summary>
/// Response DTO for draw execution endpoint
/// </summary>
public sealed record ExecuteDrawResponse(
    Guid GroupId,
    decimal Budget,
    bool DrawCompleted,
    DateTimeOffset DrawCompletedAt,
    int ParticipantCount,
    int AssignmentsCreated,
    int EmailNotificationsScheduled,
    AssignmentDto MyAssignment);

/// <summary>
/// DTO representing an assignment (Santa's recipient)
/// </summary>
public sealed record AssignmentDto(
    string RecipientId,
    string RecipientFirstName,
    string RecipientLastName);
