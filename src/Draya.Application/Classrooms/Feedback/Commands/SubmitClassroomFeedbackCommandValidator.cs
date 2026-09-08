using FluentValidation;

namespace Draya.Application.Classrooms.Feedback.Commands;

public class SubmitClassroomFeedbackCommandValidator : AbstractValidator<SubmitClassroomFeedbackCommand>
{
    public SubmitClassroomFeedbackCommandValidator()
    {
        RuleFor(x => x.Rating)
            .InclusiveBetween(1, 5).WithMessage("Rating must be between 1 and 5.");

        RuleFor(x => x.Comment)
            .MaximumLength(1000).WithMessage("Comment cannot exceed 1000 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.Comment));
    }
}
