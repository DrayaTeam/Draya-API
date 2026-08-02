using FluentValidation;

namespace Draya.Application.Classrooms.Commands.UpdateClassroom;

public class UpdateClassroomCommandValidator : AbstractValidator<UpdateClassroomCommand>
{
    public UpdateClassroomCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Classroom name is required.")
            .MaximumLength(200).WithMessage("Classroom name cannot exceed 200 characters.");

        RuleFor(x => x.SubjectId)
            .NotEmpty().WithMessage("Subject ID is required.");
    }
}
