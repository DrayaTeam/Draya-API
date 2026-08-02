using Draya.Application.Classrooms.DTOs;
using Draya.Application.Classrooms.Queries.GetClassroomRoster;
using Draya.Domain.Classrooms;
using Draya.Domain.Identity;
using Draya.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Draya.Infrastructure.Classrooms;

public class StudentRosterService : IStudentRosterService
{
    private readonly ApplicationDbContext _context;

    public StudentRosterService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<StudentRosterItemDto>> MapToRosterItemsAsync(List<Enrollment> enrollments, CancellationToken cancellationToken)
    {
        var studentIds = enrollments.Select(e => e.StudentId).ToList();
        
        var students = await _context.Students
            .Where(s => studentIds.Contains(s.UserId))
            .ToDictionaryAsync(s => s.UserId, cancellationToken);

        var items = enrollments.Select(e =>
        {
            var student = students.GetValueOrDefault(e.StudentId);
            return new StudentRosterItemDto(
                e.StudentId,
                student?.FullName ?? "Unknown",
                e.EnrolledAt,
                e.Status.ToString()
            );
        }).ToList();

        return items;
    }
}
