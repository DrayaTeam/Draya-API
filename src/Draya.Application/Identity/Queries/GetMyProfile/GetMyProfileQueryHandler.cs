using Draya.Application.Identity.DTOs;
using Draya.Domain.Identity;
using Draya.Application.Subscriptions.DTOs;
using Draya.Domain.Identity.Exceptions;
using Draya.Domain.Subscriptions;
using MediatR;

namespace Draya.Application.Identity.Queries.GetMyProfile;

public class GetMyProfileQueryHandler : IRequestHandler<GetMyProfileQuery, object>
{
    private readonly IAppUserRepository _userRepository;
    private readonly ITeacherRepository _teacherRepository;
    private readonly IStudentRepository _studentRepository;
    private readonly ISubscriptionRepository _subscriptionRepository;

    public GetMyProfileQueryHandler(
        IAppUserRepository userRepository,
        ITeacherRepository teacherRepository,
        IStudentRepository studentRepository, ISubscriptionRepository subscriptionRepository)
    {
        _userRepository = userRepository;
        _teacherRepository = teacherRepository;
        _studentRepository = studentRepository;
        _subscriptionRepository = subscriptionRepository;
    }

    public async Task<object> Handle(GetMyProfileQuery request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken)
            ?? throw new UnauthorizedAccessException("User not found.");

        return user.Role switch
        {
            Role.Teacher => await BuildTeacherProfile(user, cancellationToken),
            Role.Student => await BuildStudentProfile(user, cancellationToken),
            _ => new UserSummaryDto(user.Id, "Admin", user.Role.ToString())
        };
    }

    private async Task<TeacherProfileDto> BuildTeacherProfile(AppUser user, CancellationToken ct)
    {
        var teacher = await _teacherRepository.GetByUserIdAsync(user.Id, ct)
            ?? throw new UnauthorizedAccessException("Teacher profile not found.");
        var subscription = await _subscriptionRepository.GetActiveForTeacherAsync(user.Id, ct) ?? throw new NotFoundException("Active subscription not found.");
        var plan = subscription.Plan;
        return new TeacherProfileDto(user.Id, user.Email, teacher.FullName, teacher.Phone, new SubscriptionPlanSummaryDto(plan.Name, plan.MaxStudents, plan.MaxStorageMB, plan.MonthlyExamQuota, plan.PriceMonthly));
    }

    private async Task<StudentProfileDto> BuildStudentProfile(AppUser user, CancellationToken ct)
    {
        var student = await _studentRepository.GetByUserIdAsync(user.Id, ct)
            ?? throw new UnauthorizedAccessException("Student profile not found.");
        return new StudentProfileDto(user.Id, user.Email, student.FullName, student.ParentGuardianEmail, student.DateOfBirth);
    }
}
