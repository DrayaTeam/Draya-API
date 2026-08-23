using System;

namespace Draya.Application.Exams.DTOs;

public record StudentExamAttemptSummaryDto(
    Guid Id,
    decimal? FinalScore,
    bool NeedsTeacherReview,
    DateTime? SubmittedAt,
    DateTime StartedAt
);
