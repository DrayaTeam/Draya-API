using FluentValidation;

namespace Draya.Application.Identity.Commands.UpdateStudentProfile;

public class UpdateStudentProfileCommandValidator : AbstractValidator<UpdateStudentProfileCommand>
{
    public UpdateStudentProfileCommandValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Full name is required.")
            .MaximumLength(200).WithMessage("Full name must not exceed 200 characters.");

        RuleFor(x => x.FullName)
            .Must(name => !string.IsNullOrWhiteSpace(name) && name.Trim().Length >= 7)
            .WithMessage("Full name must be at least 7 characters.")
            .Matches("^[\\p{L}'\\-\\s]+$")
            .WithMessage("Full name must contain only letters, spaces, hyphens or apostrophes.");

        RuleFor(x => x.ParentGuardianEmail)
            .NotEmpty().WithMessage("Parent/guardian email is required.")
            .EmailAddress().WithMessage("A valid parent/guardian email address is required.");

        RuleFor(x => x.DateOfBirth)
            .NotNull().WithMessage("Date of birth is required for students.")
            .Must(dob =>
            {
                if (!dob.HasValue) return false;
                var today = DateTime.UtcNow.Date;
                var birth = dob.Value.Date;
                if (birth >= today) return false;
                var age = today.Year - birth.Year;
                if (birth > today.AddYears(-age)) age--;
                return age > 6;
            })
            .WithMessage("Student must be older than 6 years.");
    }
}
