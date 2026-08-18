using System;
using Draya.Domain.Exams;
using MediatR;

namespace Draya.Application.Exams.Events;

public class ExamGenerationProgressEvent : INotification
{
    public Guid GenerationId { get; }
    public Guid TeacherId { get; }
    public GenerationStatus Status { get; }
    public string? ErrorMessage { get; }
    public Guid? ExamId { get; }

    public ExamGenerationProgressEvent(Guid generationId, Guid teacherId, GenerationStatus status, string? errorMessage = null, Guid? examId = null)
    {
        GenerationId = generationId;
        TeacherId = teacherId;
        Status = status;
        ErrorMessage = errorMessage;
        ExamId = examId;
    }
}
