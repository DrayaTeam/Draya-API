using FluentValidation;

namespace Draya.Application.Classrooms.Commands.DeleteSection;

public class DeleteSectionCommandValidator : AbstractValidator<DeleteSectionCommand>
{
    public DeleteSectionCommandValidator()
    {
        RuleFor(x => x.SectionId)
            .NotEmpty().WithMessage("Section ID is required.");
    }
}
