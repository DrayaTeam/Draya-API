using System.Security.Claims;
using Draya.Domain.Classrooms;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Draya.Api.Notifications;

[Authorize]
public class ClassroomQaHub : Hub
{
    private readonly IClassroomRepository _classroomRepository;
    private readonly IEnrollmentRepository _enrollmentRepository;

    public ClassroomQaHub(IClassroomRepository classroomRepository, IEnrollmentRepository enrollmentRepository)
    {
        _classroomRepository = classroomRepository;
        _enrollmentRepository = enrollmentRepository;
    }

    public async Task JoinClassroom(Guid classroomId)
    {
        var userIdString = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? Context.User?.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub);

        if (Guid.TryParse(userIdString, out var userId))
        {
            var classroom = await _classroomRepository.GetByIdAsync(classroomId);
            if (classroom != null)
            {
                if (classroom.TeacherId == userId)
                {
                    await Groups.AddToGroupAsync(Context.ConnectionId, $"classroom_{classroomId}");
                    return;
                }

                var enrollment = await _enrollmentRepository.GetByStudentAndClassroomAsync(userId, classroomId);
                if (enrollment != null && enrollment.Status == EnrollmentStatus.Active)
                {
                    await Groups.AddToGroupAsync(Context.ConnectionId, $"classroom_{classroomId}");
                }
            }
        }
    }

    public async Task LeaveClassroom(Guid classroomId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"classroom_{classroomId}");
    }
}
