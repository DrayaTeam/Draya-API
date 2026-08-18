using Draya.Application.Classrooms.Feedback.DTOs;
using Draya.Domain.Classrooms;
using Draya.Domain.Classrooms.Exceptions;
using Draya.Domain.Identity;
using MediatR;

namespace Draya.Application.Classrooms.Feedback.Queries;

public class GetClassroomFeedbackSummaryQueryHandler : IRequestHandler<GetClassroomFeedbackSummaryQuery, ClassroomFeedbackSummaryDto>
{
    private readonly IClassroomRepository _classroomRepository;
    private readonly IClassroomFeedbackRepository _feedbackRepository;
    private readonly IStudentRepository _studentRepository;

    public GetClassroomFeedbackSummaryQueryHandler(
        IClassroomRepository classroomRepository,
        IClassroomFeedbackRepository feedbackRepository,
        IStudentRepository studentRepository)
    {
        _classroomRepository = classroomRepository;
        _feedbackRepository = feedbackRepository;
        _studentRepository = studentRepository;
    }

    public async Task<ClassroomFeedbackSummaryDto> Handle(GetClassroomFeedbackSummaryQuery request, CancellationToken cancellationToken)
    {
        var classroom = await _classroomRepository.GetByIdAsync(request.ClassroomId, cancellationToken);
        if (classroom is null || classroom.TeacherId != request.TeacherId)
        {
            throw new ClassroomNotFoundException();
        }

        var pageNumber = Math.Max(1, request.PageNumber);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var (feedbackItems, totalCount, averageRating) = await _feedbackRepository.GetByClassroomIdAsync(
            request.ClassroomId,
            pageNumber,
            pageSize,
            cancellationToken);

        var studentIds = feedbackItems.Select(f => f.StudentId).Distinct().ToList();
        var students = (await _studentRepository.GetByUserIdsAsync(studentIds, cancellationToken))
            .ToDictionary(s => s.UserId);

        var items = feedbackItems.Select(f =>
        {
            students.TryGetValue(f.StudentId, out var student);
            return new ClassroomFeedbackItemDto(
                f.Id,
                student?.FullName ?? string.Empty,
                student?.ProfilePictureUrl,
                f.Rating,
                f.Comment,
                f.CreatedAt);
        }).ToList();

        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        return new ClassroomFeedbackSummaryDto(
            Math.Round(averageRating, 1),
            totalCount,
            items,
            pageNumber,
            pageSize,
            totalPages,
            pageNumber < totalPages,
            pageNumber > 1);
    }
}
