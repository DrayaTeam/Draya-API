using FluentValidation;

namespace Draya.Application.Classrooms.Commands.EnrollStudent;

public class EnrollStudentCommandValidator : AbstractValidator<EnrollStudentCommand>
{
    public EnrollStudentCommandValidator()
    {
        RuleFor(x => x.EnrollmentCode)
            .NotEmpty().WithMessage("Enrollment code is required.");
    }
}
