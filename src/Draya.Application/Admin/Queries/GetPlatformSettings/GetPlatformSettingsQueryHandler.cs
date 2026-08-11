using Draya.Application.Admin.DTOs;
using Draya.Domain.Admin;
using MediatR;

namespace Draya.Application.Admin.Queries.GetPlatformSettings;

public record GetPlatformSettingsQuery : IRequest<PlatformSettingDto>;

public class GetPlatformSettingsQueryHandler : IRequestHandler<GetPlatformSettingsQuery, PlatformSettingDto>
{
    private readonly IPlatformSettingRepository _settingRepository;

    public GetPlatformSettingsQueryHandler(IPlatformSettingRepository settingRepository)
    {
        _settingRepository = settingRepository;
    }

    public async Task<PlatformSettingDto> Handle(GetPlatformSettingsQuery request, CancellationToken cancellationToken)
    {
        var settings = await _settingRepository.GetSettingsAsync(cancellationToken);
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
