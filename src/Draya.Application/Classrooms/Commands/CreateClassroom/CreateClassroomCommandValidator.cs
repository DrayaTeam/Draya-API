using FluentValidation;

namespace Draya.Application.Classrooms.Commands.CreateClassroom;

public class CreateClassroomCommandValidator : AbstractValidator<CreateClassroomCommand>
{
    public CreateClassroomCommandValidator()
    {
        RuleFor(x => x.SubjectId)
            .NotEmpty().WithMessage("Subject ID is required.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Classroom name is required.")
            .MaximumLength(200).WithMessage("Classroom name cannot exceed 200 characters.");
    }
}
