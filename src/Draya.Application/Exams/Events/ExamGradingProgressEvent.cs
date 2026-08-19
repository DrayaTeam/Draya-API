using System;
using Draya.Domain.Exams;
using MediatR;

namespace Draya.Application.Exams.Events;

public record ExamGradingProgressEvent(
    Guid GradingJobId,
    Guid StudentId,
    GradingStatus Status,
    string? ErrorMessage,
    decimal? FinalScore,
    bool NeedsTeacherReview
) : INotification;
