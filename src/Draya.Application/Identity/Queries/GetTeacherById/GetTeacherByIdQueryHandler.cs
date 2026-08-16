using Draya.Application.Identity.DTOs;
using Draya.Domain.Identity;
using MediatR;
using System.ComponentModel.DataAnnotations;

namespace Draya.Application.Identity.Queries.GetTeacherById;

public class GetTeacherByIdQueryHandler : IRequestHandler<GetTeacherByIdQuery, TeacherProfileDto>
{
    private readonly ITeacherRepository _teacherRepository;

    public GetTeacherByIdQueryHandler(ITeacherRepository teacherRepository)
    {
        _teacherRepository = teacherRepository;
    }

    public async Task<TeacherProfileDto> Handle(GetTeacherByIdQuery request, CancellationToken cancellationToken)
    {
        var teacher = await _teacherRepository.GetByUserIdAsync(request.TeacherId, cancellationToken);

        if (teacher == null)
        {
            throw new ValidationException("Teacher not found."); // Should ideally use a NotFoundException
        }

        return new TeacherProfileDto(
            teacher.UserId,
            string.Empty,
            teacher.FullName,
            teacher.Phone,
            teacher.Specialization,
            teacher.Description,
            teacher.ProfilePictureUrl
        );
    }
}

