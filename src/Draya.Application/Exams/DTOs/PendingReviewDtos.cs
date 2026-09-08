using System;
using System.Collections.Generic;

namespace Draya.Application.Exams.DTOs;

public record PendingReviewAttemptDto(
    Guid AttemptId,
    Guid StudentId,
    string StudentName,
    DateTime? SubmittedAt,
    decimal? Score);

public record PendingReviewExamDto(
    Guid ExamId,
    string ExamTitle,
    List<PendingReviewAttemptDto> PendingReviews);

public record PendingReviewClassroomDto(
    Guid ClassroomId,
    string ClassroomName,
    List<PendingReviewExamDto> Exams);
