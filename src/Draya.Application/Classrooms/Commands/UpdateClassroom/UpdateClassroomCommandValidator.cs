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

        RuleFor(x => x.ClassroomTypeId)
            .NotEmpty().WithMessage("Classroom Type ID is required.");

        RuleFor(x => x.GradeLevelId)
            .NotEmpty().WithMessage("Grade Level ID is required.");

        RuleFor(x => x.StartDate)
            .NotEmpty().WithMessage("Start Date is required.");

        RuleFor(x => x.EndDate)
            .NotEmpty().WithMessage("End Date is required.")
            .GreaterThan(x => x.StartDate).WithMessage("End Date must be after Start Date.");

        RuleFor(x => x.Price)
            .GreaterThanOrEqualTo(0).WithMessage("Price must be greater than or equal to 0.");

        RuleFor(x => x.ImageUrl)
            .MaximumLength(2048).WithMessage("Image URL cannot exceed 2048 characters.")
            .Must(BeAValidUrl).WithMessage("Image URL must be a valid absolute URL.")
            .When(x => !string.IsNullOrWhiteSpace(x.ImageUrl));
    }

    private static bool BeAValidUrl(string? imageUrl)
        => Uri.TryCreate(imageUrl, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
}
