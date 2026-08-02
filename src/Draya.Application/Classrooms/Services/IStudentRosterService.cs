using Draya.Application.Classrooms.DTOs;
using Draya.Domain.Classrooms;

namespace Draya.Application.Classrooms.Queries.GetClassroomRoster;

public interface IStudentRosterService
{
    Task<List<StudentRosterItemDto>> MapToRosterItemsAsync(List<Enrollment> enrollments, CancellationToken cancellationToken);
}
