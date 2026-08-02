using Draya.Application.Subscriptions.DTOs;
using Draya.Domain.Identity.Exceptions;
using Draya.Domain.Subscriptions;
using MediatR;
namespace Draya.Application.Subscriptions.Queries;
public record GetCurrentSubscriptionQuery(Guid TeacherId) : IRequest<SubscriptionPlanSummaryDto>;
public record GetSubscriptionUsageQuery(Guid TeacherId) : IRequest<SubscriptionUsageDto>;
public class GetCurrentSubscriptionQueryHandler(ISubscriptionRepository repository) : IRequestHandler<GetCurrentSubscriptionQuery, SubscriptionPlanSummaryDto> { public async Task<SubscriptionPlanSummaryDto> Handle(GetCurrentSubscriptionQuery request, CancellationToken ct) { var s = await repository.GetActiveForTeacherAsync(request.TeacherId, ct) ?? throw new NotFoundException("Active subscription not found."); var p = s.Plan; return new(p.Name,p.MaxStudents,p.MaxStorageMB,p.MonthlyExamQuota,p.PriceMonthly); } }
public class GetSubscriptionUsageQueryHandler(ISubscriptionRepository repository) : IRequestHandler<GetSubscriptionUsageQuery, SubscriptionUsageDto> { public async Task<SubscriptionUsageDto> Handle(GetSubscriptionUsageQuery request, CancellationToken ct) { var s = await repository.GetActiveForTeacherAsync(request.TeacherId, ct) ?? throw new NotFoundException("Active subscription not found."); var u = await repository.GetUsageForTeacherAsync(request.TeacherId, new DateOnly(DateTime.UtcNow.Year,DateTime.UtcNow.Month,1), ct); var p=s.Plan; return new(u?.CurrentStudentsCount ?? 0,p.MaxStudents,u?.StorageUsedMB ?? 0,p.MaxStorageMB,u?.ExamsGeneratedCount ?? 0,p.MonthlyExamQuota); } }
