using Draya.Application.Classrooms.DTOs;
using Draya.Domain.Classrooms;
using Draya.Domain.Classrooms.Exceptions;
using MediatR;

namespace Draya.Application.Classrooms.Queries.GetClassroomRoster;

public class GetClassroomRosterQueryHandler : IRequestHandler<GetClassroomRosterQuery, PagedResult<StudentRosterItemDto>>
{
    private readonly IClassroomRepository _classroomRepository;
    private readonly IEnrollmentRepository _enrollmentRepository;
    private readonly IStudentRosterService _studentRosterService;

    public GetClassroomRosterQueryHandler(
        IClassroomRepository classroomRepository,
        IEnrollmentRepository enrollmentRepository,
        IStudentRosterService studentRosterService)
    {
        _classroomRepository = classroomRepository;
        _enrollmentRepository = enrollmentRepository;
        _studentRosterService = studentRosterService;
    }

    public async Task<PagedResult<StudentRosterItemDto>> Handle(GetClassroomRosterQuery request, CancellationToken cancellationToken)
    {
        var classroom = await _classroomRepository.GetByIdAsync(request.ClassroomId, cancellationToken);
        
        if (classroom == null || classroom.TeacherId != request.TeacherId)
        {
            throw new ClassroomNotFoundException();
        }

        var enrollments = await _enrollmentRepository.GetByClassroomIdAsync(
            request.ClassroomId,
            request.Page,
            request.PageSize,
            cancellationToken);

        var totalCount = await _enrollmentRepository.GetCountByClassroomIdAsync(
            request.ClassroomId,
            cancellationToken);

        var totalPages = (int)Math.Ceiling((double)totalCount / request.PageSize);

        var items = await _studentRosterService.MapToRosterItemsAsync(enrollments, cancellationToken);

        return new PagedResult<StudentRosterItemDto>(
            items,
            request.Page,
            request.PageSize,
            totalCount,
            totalPages
        );
    }
}
