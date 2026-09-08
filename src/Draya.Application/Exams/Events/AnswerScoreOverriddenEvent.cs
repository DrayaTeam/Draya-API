using System;
using MediatR;

namespace Draya.Application.Exams.Events;

public class AnswerScoreOverriddenEvent : INotification
{
    public Guid StudentId { get; }
    public Guid AttemptId { get; }
    public Guid AnswerId { get; }
    public decimal NewScore { get; }

    public AnswerScoreOverriddenEvent(Guid studentId, Guid attemptId, Guid answerId, decimal newScore)
    {
        StudentId = studentId;
        AttemptId = attemptId;
        AnswerId = answerId;
        NewScore = newScore;
    }
}
