using Draya.Application.Admin.DTOs;
using Draya.Domain.Admin;
using MediatR;

namespace Draya.Application.Admin.Queries.GetFinancialOverview;

public record GetFinancialOverviewQuery : IRequest<FinancialOverviewDto>;

public class GetFinancialOverviewQueryHandler : IRequestHandler<GetFinancialOverviewQuery, FinancialOverviewDto>
{
    private readonly IFinancialOverviewRepository _financialOverviewRepository;

    public GetFinancialOverviewQueryHandler(IFinancialOverviewRepository financialOverviewRepository)
    {
        _financialOverviewRepository = financialOverviewRepository;
    }

    public async Task<FinancialOverviewDto> Handle(GetFinancialOverviewQuery request, CancellationToken cancellationToken)
    {
        var data = await _financialOverviewRepository.GetFinancialOverviewAsync(cancellationToken);
        return new FinancialOverviewDto(
            data.TotalClassroomRevenue,
            data.TotalCommissionCollected,
            data.TotalTeacherEarnedBalance,
            data.TotalTeacherPurchasedBalance,
            data.TotalOutstandingEarnedBalance,
            data.TotalTopUps,
            data.TotalAIExamCharges
        );
    }
}
