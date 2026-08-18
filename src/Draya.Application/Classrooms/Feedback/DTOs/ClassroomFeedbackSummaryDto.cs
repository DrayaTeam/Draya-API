namespace Draya.Application.Classrooms.Feedback.DTOs;

public record ClassroomFeedbackSummaryDto(
    double AverageRating,
    int TotalCount,
    List<ClassroomFeedbackItemDto> Items,
    int PageNumber,
    int PageSize,
    int TotalPages,
    bool HasNextPage,
    bool HasPreviousPage
);
