using FluentValidation;

namespace Draya.Application.Identity.Commands.RegisterStudent;

public class RegisterStudentCommandValidator : AbstractValidator<RegisterStudentCommand>
{
    public RegisterStudentCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("A valid email address is required.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters.")
            .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain at least one number.");

        RuleFor(x => x.ConfirmPassword)
            .Equal(x => x.Password).WithMessage("Passwords do not match.");

        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Full name is required.")
            .MaximumLength(200).WithMessage("Full name must not exceed 200 characters.");

        // Ensure full name is trimmed, has at least 7 characters, and contains only letters, spaces, hyphens or apostrophes
        RuleFor(x => x.FullName)
            .Must(name => !string.IsNullOrWhiteSpace(name) && name.Trim().Length >= 7)
            .WithMessage("Full name must be at least 7 characters.")
            .Matches("^[\\p{L}'\\-\\s]+$")
            .WithMessage("Full name must contain only letters, spaces, hyphens or apostrophes.");

        RuleFor(x => x.ParentGuardianEmail)
            .NotEmpty().WithMessage("Parent/guardian email is required.")
            .EmailAddress().WithMessage("A valid parent/guardian email address is required.")
            .Must((command, parentEmail) =>
            {
                if (string.IsNullOrWhiteSpace(parentEmail)) return false;
                return !string.Equals(parentEmail.Trim(), command.Email?.Trim(), System.StringComparison.OrdinalIgnoreCase);
            })
            .WithMessage("Parent/guardian email must be different from the student's email.");

        RuleFor(x => x.ParentGuardianName)
            .NotEmpty().WithMessage("Parent/guardian name is required.")
            .MinimumLength(3).WithMessage("Parent/guardian name must be at least 3 characters.")
            .MaximumLength(100).WithMessage("Parent/guardian name must not exceed 100 characters.");

        RuleFor(x => x.ParentGuardianPhone)
            .NotEmpty().WithMessage("Parent/guardian phone is required.")
            .Matches("^01[0-2,5][0-9]{8}$").WithMessage("Invalid Egyptian mobile number format.");

        RuleFor(x => x.DateOfBirth)
            .NotNull().WithMessage("Date of birth is required for students.")
            .Must(dob =>
            {
                if (!dob.HasValue) return false;
                var today = DateTime.UtcNow.Date;
                var birth = dob.Value.Date;
                if (birth >= today) return false; // cannot be today or in future
                var age = today.Year - birth.Year;
                if (birth > today.AddYears(-age)) age--;
                return age > 6; // strictly greater than 6 years old
            })
            .WithMessage("Student must be older than 6 years.");
    }
}
