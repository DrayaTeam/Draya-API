using Draya.Application.Identity.DTOs;
using Draya.Domain.Identity;
using MediatR;
using System.Linq;

namespace Draya.Application.Identity.Queries.GetTeachers;

public class GetTeachersQueryHandler : IRequestHandler<GetTeachersQuery, List<TeacherProfileDto>>
{
    private readonly ITeacherRepository _teacherRepository;

    public GetTeachersQueryHandler(ITeacherRepository teacherRepository)
    {
        _teacherRepository = teacherRepository;
    }

    public async Task<List<TeacherProfileDto>> Handle(GetTeachersQuery request, CancellationToken cancellationToken)
    {
        var teachers = await _teacherRepository.GetAllAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(request.Specialization))
        {
            teachers = teachers.Where(t => 
                t.Specialization != null && 
                t.Specialization.Contains(request.Specialization, StringComparison.OrdinalIgnoreCase));
        }

        return teachers.Select(t => new TeacherProfileDto(
            t.UserId,
            string.Empty,
            t.FullName,
            t.Phone,
            t.Specialization,
            t.Description
        )).ToList();
    }
}
