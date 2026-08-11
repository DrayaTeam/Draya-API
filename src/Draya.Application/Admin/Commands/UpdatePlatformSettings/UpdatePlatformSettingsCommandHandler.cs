using Draya.Application.Admin.DTOs;
using Draya.Domain.Admin;
using MediatR;

namespace Draya.Application.Admin.Commands.UpdatePlatformSettings;

public record UpdatePlatformSettingsCommand(
    decimal AIExamPrice,
    int FreeMonthlyAIExamQuota,
    decimal PlatformCommissionPercent,
    Guid AdminId
) : IRequest<PlatformSettingDto>;

public class UpdatePlatformSettingsCommandHandler : IRequestHandler<UpdatePlatformSettingsCommand, PlatformSettingDto>
{
    private readonly IPlatformSettingRepository _settingRepository;

    public UpdatePlatformSettingsCommandHandler(IPlatformSettingRepository settingRepository)
    {
        _settingRepository = settingRepository;
    }

    public async Task<PlatformSettingDto> Handle(UpdatePlatformSettingsCommand request, CancellationToken cancellationToken)
    {
        if (request.AIExamPrice < 0)
        {
            throw new ArgumentException("AI Exam Price must be greater than or equal to zero.", nameof(request.AIExamPrice));
        }

        if (request.FreeMonthlyAIExamQuota < 0)
        {
            throw new ArgumentException("Free Monthly Quota must be greater than or equal to zero.", nameof(request.FreeMonthlyAIExamQuota));
        }

        if (request.PlatformCommissionPercent < 0 || request.PlatformCommissionPercent > 100)
        {
            throw new ArgumentException("Commission percent must be between 0 and 100.", nameof(request.PlatformCommissionPercent));
        }

        var settings = await _settingRepository.GetSettingsAsync(cancellationToken);
        settings.AIExamPrice = request.AIExamPrice;
        settings.FreeMonthlyAIExamQuota = request.FreeMonthlyAIExamQuota;
        settings.PlatformCommissionPercent = request.PlatformCommissionPercent;
        settings.UpdatedByAdminId = request.AdminId;

        await _settingRepository.UpdateSettingsAsync(settings, cancellationToken);
        await _settingRepository.SaveChangesAsync(cancellationToken);

        return new PlatformSettingDto(
            settings.Id,
            settings.AIExamPrice,
            settings.FreeMonthlyAIExamQuota,
            settings.PlatformCommissionPercent,
            settings.UpdatedAt,
            settings.UpdatedByAdminId
        );
    }
}
