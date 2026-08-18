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

        string returnedEnrollmentCode = classroom.EnrollmentCode;

        if (!isAuthorized)
        {
            if (request.UserRole == "Student" && classroom.IsActive)
            {
                // Unenrolled student viewing public details
                returnedEnrollmentCode = string.Empty;
            }
            else
            {
                throw new ClassroomNotFoundException();
            }
        }

        return new ClassroomDto(
            classroom.Id,
            classroom.TeacherId,
            classroom.Subject?.Name ?? string.Empty,
            classroom.Name,
            returnedEnrollmentCode,
            classroom.IsActive,
            0,
            classroom.CreatedAt,
            classroom.ClassroomType?.Name ?? string.Empty,
            classroom.GradeLevel?.Name ?? string.Empty,
            classroom.StartDate,
            classroom.EndDate,
            classroom.Price,
            classroom.ImageUrl
        );
    }
}
