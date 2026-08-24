using System;
using System.Collections.Generic;
using Draya.Application.Exams.Services;
using FluentValidation;

namespace Draya.Application.Exams.Validators;

public class GenerateExamRequestValidator : AbstractValidator<GenerateExamRequest>
{
    private static readonly HashSet<string> ValidTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "MCQ", "MultipleChoice", "Essay", "TrueFalse", "FillInTheBlank", "ShortAnswer"
    };

    private static readonly HashSet<string> ValidDifficulties = new(StringComparer.OrdinalIgnoreCase)
    {
        "Easy", "Medium", "Hard"
    };

    public GenerateExamRequestValidator()
    {
        RuleFor(x => x.Topic)
            .NotEmpty().WithMessage("Topic is required.")
            .MaximumLength(200).WithMessage("Topic must not exceed 200 characters.");

        RuleFor(x => x.DifficultyLevel)
            .NotEmpty().WithMessage("Difficulty level is required.")
            .Must(d => ValidDifficulties.Contains(d))
            .WithMessage("DifficultyLevel must be one of: Easy, Medium, Hard.");

        RuleFor(x => x.DurationMinutes)
            .GreaterThan(0).WithMessage("DurationMinutes must be greater than 0.")
            .LessThanOrEqualTo(600).WithMessage("DurationMinutes must not exceed 600 (10 hours).");

        RuleFor(x => x.StartDate)
            .NotEmpty().WithMessage("StartDate is required.")
            .Must(d => d >= DateTime.UtcNow.AddMinutes(-5)).WithMessage("StartDate must be in the future (or very recently started).");

        RuleFor(x => x.EndDate)
            .GreaterThan(x => x.StartDate)
            .WithMessage("EndDate must be after StartDate.")
            .When(x => x.EndDate.HasValue);

        RuleFor(x => x.AllowedAttempts)
            .GreaterThan(0).WithMessage("AllowedAttempts must be at least 1.");

        RuleFor(x => x.IdempotencyKey)
            .NotEmpty().WithMessage("IdempotencyKey is required.");

        RuleFor(x => x.QuestionRequirements)
            .NotEmpty().WithMessage("At least one question requirement must be specified.");

        RuleFor(x => x.QuestionRequirements)
            .Must(reqs => reqs.Sum(r => r.Count) <= 50)
            .WithMessage("Total question count must not exceed 50.")
            .When(x => x.QuestionRequirements?.Count > 0);

        RuleForEach(x => x.QuestionRequirements)
            .ChildRules(req =>
            {
                req.RuleFor(r => r.Type)
                    .NotEmpty().WithMessage("Question type is required.")
                    .Must(t => ValidTypes.Contains(t))
                    .WithMessage(r => $"'{r.Type}' is not a valid question type. Valid types: MCQ, Essay, TrueFalse, FillInTheBlank, ShortAnswer.");

                req.RuleFor(r => r.Count)
                    .GreaterThan(0).WithMessage("Question count must be greater than 0.");
            });
    }
}
