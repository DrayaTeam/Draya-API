namespace Draya.Application.Identity.DTOs;
using Draya.Application.Subscriptions.DTOs;

public record TeacherProfileDto(
    Guid UserId,
    string Email,
    string FullName,
    string? Phone,
    SubscriptionPlanSummaryDto CurrentPlan
);
