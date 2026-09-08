using System;
using System.Threading;
using System.Threading.Tasks;
using Draya.Application.Exams.DTOs;
using Draya.Domain.Admin;
using Draya.Domain.Subscriptions;
using Draya.Domain.Wallets;
using MediatR;

namespace Draya.Application.Exams.Queries.GetTeacherAIExamQuota;

public class GetTeacherAIExamQuotaQueryHandler : IRequestHandler<GetTeacherAIExamQuotaQuery, AIExamQuotaDto>
{
    private readonly IPlatformSettingRepository _settingRepository;
    private readonly IUsageCounterRepository _usageRepository;
    private readonly ITeacherWalletRepository _walletRepository;

    public GetTeacherAIExamQuotaQueryHandler(
        IPlatformSettingRepository settingRepository,
        IUsageCounterRepository usageRepository,
        ITeacherWalletRepository walletRepository)
    {
        _settingRepository = settingRepository;
        _usageRepository = usageRepository;
        _walletRepository = walletRepository;
    }

    public async Task<AIExamQuotaDto> Handle(GetTeacherAIExamQuotaQuery request, CancellationToken cancellationToken)
    {
        var settings = await _settingRepository.GetSettingsAsync(cancellationToken)
            ?? new PlatformSetting { FreeMonthlyAIExamQuota = 3, AIExamPrice = 20.00m };

        var currentMonth = new DateOnly(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
        var usageCounter = await _usageRepository.GetForTeacherAsync(request.TeacherId, currentMonth, cancellationToken);
        var freeUsed = usageCounter?.FreeExamsUsed ?? 0;
        var remainingFree = Math.Max(0, settings.FreeMonthlyAIExamQuota - freeUsed);

        var wallet = await _walletRepository.GetByTeacherIdAsync(request.TeacherId, cancellationToken);
        var combinedBalance = (wallet?.EarnedBalance ?? 0m) + (wallet?.PurchasedBalance ?? 0m);
        var hasSufficientBalance = combinedBalance >= settings.AIExamPrice;

        return new AIExamQuotaDto(
            settings.FreeMonthlyAIExamQuota,
            freeUsed,
            remainingFree,
            settings.AIExamPrice,
            hasSufficientBalance
        );
    }
}
