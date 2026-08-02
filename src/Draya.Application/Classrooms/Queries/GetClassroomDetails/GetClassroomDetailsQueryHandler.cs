using Draya.Application.Classrooms.DTOs;
using Draya.Domain.Classrooms;
using Draya.Domain.Classrooms.Exceptions;
using MediatR;

namespace Draya.Application.Classrooms.Queries.GetClassroomDetails;

public class GetClassroomDetailsQueryHandler : IRequestHandler<GetClassroomDetailsQuery, ClassroomDto>
{
    private readonly IClassroomRepository _classroomRepository;

    public GetClassroomDetailsQueryHandler(IClassroomRepository classroomRepository)
    {
        _classroomRepository = classroomRepository;
    }

    public async Task<ClassroomDto> Handle(GetClassroomDetailsQuery request, CancellationToken cancellationToken)
    {
        var classroom = await _classroomRepository.GetByIdAsync(request.ClassroomId, cancellationToken);
        
        if (classroom == null)
        {
            throw new ClassroomNotFoundException();
        }

        var isAuthorized = false;

        if (request.UserRole == "Teacher")
        {
            isAuthorized = classroom.TeacherId == request.UserId;
        }
        else if (request.UserRole == "Student")
        {
            isAuthorized = await _classroomRepository.IsStudentEnrolledAsync(
                request.UserId,
                request.ClassroomId,
                cancellationToken);
        }

        if (!isAuthorized)
        {
            throw new ClassroomNotFoundException();
        }

        return new ClassroomDto(
            classroom.Id,
            classroom.TeacherId,
            classroom.Subject?.Name ?? string.Empty,
            classroom.Name,
            classroom.EnrollmentCode,
            classroom.IsActive,
            0,
            classroom.CreatedAt
        );
    }
}
