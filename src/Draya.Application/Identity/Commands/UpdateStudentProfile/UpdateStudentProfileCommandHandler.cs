using Draya.Domain.Identity;
using MediatR;
using System.ComponentModel.DataAnnotations;

namespace Draya.Application.Identity.Commands.UpdateStudentProfile;

public class UpdateStudentProfileCommandHandler : IRequestHandler<UpdateStudentProfileCommand>
{
    private readonly IStudentRepository _studentRepository;

    public UpdateStudentProfileCommandHandler(IStudentRepository studentRepository)
    {
        _studentRepository = studentRepository;
    }

    public async Task Handle(UpdateStudentProfileCommand request, CancellationToken cancellationToken)
    {
        var student = await _studentRepository.GetByUserIdAsync(request.UserId, cancellationToken);
        if (student is null)
        {
            throw new ValidationException("Student profile not found.");
        }

        student.FullName = request.FullName.Trim();
        student.ParentGuardianEmail = request.ParentGuardianEmail.Trim().ToLowerInvariant();
        student.ParentGuardianName = request.ParentGuardianName?.Trim() ?? string.Empty;
        student.ParentGuardianPhone = request.ParentGuardianPhone?.Trim() ?? string.Empty;
        student.DateOfBirth = request.DateOfBirth;

        await _studentRepository.SaveChangesAsync(cancellationToken);
    }
}
