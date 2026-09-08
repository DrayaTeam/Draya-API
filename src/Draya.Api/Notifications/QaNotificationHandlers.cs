using Draya.Application.Classrooms.Questions.Notifications;
using MediatR;
using Microsoft.AspNetCore.SignalR;

namespace Draya.Api.Notifications;

public class QaNotificationHandlers : 
    INotificationHandler<QuestionCreatedNotification>,
    INotificationHandler<QuestionRepliedNotification>,
    INotificationHandler<QuestionVoteUpdatedNotification>
{
    private readonly IHubContext<ClassroomQaHub> _hubContext;

    public QaNotificationHandlers(IHubContext<ClassroomQaHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task Handle(QuestionCreatedNotification notification, CancellationToken cancellationToken)
    {
        var groupName = $"classroom_{notification.ClassroomId}";
        await _hubContext.Clients.Group(groupName).SendAsync("QuestionCreated", notification, cancellationToken);
    }

    public async Task Handle(QuestionRepliedNotification notification, CancellationToken cancellationToken)
    {
        var groupName = $"classroom_{notification.ClassroomId}";
        await _hubContext.Clients.Group(groupName).SendAsync("QuestionReplied", notification, cancellationToken);
    }

    public async Task Handle(QuestionVoteUpdatedNotification notification, CancellationToken cancellationToken)
    {
        var groupName = $"classroom_{notification.ClassroomId}";
        await _hubContext.Clients.Group(groupName).SendAsync("QuestionVoteUpdated", notification, cancellationToken);
    }
}
