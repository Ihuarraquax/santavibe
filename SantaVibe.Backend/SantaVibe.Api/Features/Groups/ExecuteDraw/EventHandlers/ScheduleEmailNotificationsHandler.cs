using MediatR;
using SantaVibe.Api.Data;
using SantaVibe.Api.Data.Entities;
using SantaVibe.Api.Data.Entities.Events;

namespace SantaVibe.Api.Features.Groups.ExecuteDraw.EventHandlers;

/// <summary>
/// Domain event handler that schedules email notifications when draw is completed
/// </summary>
public sealed class ScheduleEmailNotificationsHandler(
    ApplicationDbContext context,
    ILogger<ScheduleEmailNotificationsHandler> logger)
    : INotificationHandler<DrawCompletedEvent>
{
    public async Task Handle(DrawCompletedEvent notification, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Scheduling email notifications for group {GroupId} with {ParticipantCount} participants",
            notification.GroupId,
            notification.ParticipantIds.Count);

        // Create email notification records for all participants
        var emailNotifications = notification.ParticipantIds.Select(userId => new EmailNotification
        {
            Id = Guid.NewGuid(),
            Type = EmailNotificationType.DrawCompleted,
            RecipientUserId = userId,
            GroupId = notification.GroupId,
            ScheduledAt = notification.OccurredAt,
            SentAt = null,
            FirstAttemptAt = null,
            LastAttemptAt = null,
            AttemptCount = 0,
            LastError = null
        }).ToList();

        // Add notifications to database
        // Note: Don't call SaveChangesAsync here - we're running within the same transaction
        // as the ExecuteDrawHandler. The TransactionBehavior will commit the transaction.
        await context.EmailNotifications.AddRangeAsync(emailNotifications, cancellationToken);

        logger.LogInformation(
            "Scheduled {EmailCount} email notifications for group {GroupId}",
            emailNotifications.Count,
            notification.GroupId);
    }
}
