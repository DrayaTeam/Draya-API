using Draya.Domain.Identity;
using MediatR;
using System.ComponentModel.DataAnnotations;

namespace Draya.Application.Identity.Commands.UpdateTeacherProfile;

public class UpdateTeacherProfileCommandHandler : IRequestHandler<UpdateTeacherProfileCommand>
{
    private readonly ITeacherRepository _teacherRepository;

    public UpdateTeacherProfileCommandHandler(ITeacherRepository teacherRepository)
    {
        _teacherRepository = teacherRepository;
    }

    public async Task Handle(UpdateTeacherProfileCommand request, CancellationToken cancellationToken)
    {
        var teacher = await _teacherRepository.GetByUserIdAsync(request.UserId, cancellationToken);
        if (teacher is null)
        {
            throw new ValidationException("Teacher profile not found.");
        }

        teacher.FullName = request.FullName.Trim();
        teacher.Phone = request.Phone?.Trim();
        teacher.Specialization = request.Specialization?.Trim();
        teacher.Description = request.Description?.Trim();

        await _teacherRepository.SaveChangesAsync(cancellationToken);
    }
}
