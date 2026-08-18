using Draya.Application.Classrooms.Feedback.DTOs;
using MediatR;

namespace Draya.Application.Classrooms.Feedback.Queries;

public record GetClassroomFeedbackSummaryQuery(
    Guid ClassroomId,
    Guid TeacherId,
    int PageNumber,
    int PageSize
) : IRequest<ClassroomFeedbackSummaryDto>;
