using System;

namespace Draya.Application.Reports.DTOs;

public record WeaknessDto(
    Guid Id,
    Guid TopicId,
    string TopicName,
    decimal CurrentProficiencyPercent,
    DateTime LastUpdatedAt
);

public record ResolvedWeaknessDto(
    Guid Id,
    Guid TopicId,
    string TopicName,
    decimal CurrentProficiencyPercent,
    decimal PreviousProficiencyPercent,
    decimal Delta,
    DateTime LastUpdatedAt
);

public record WeaknessHistoryDto(
    Guid HistoryId,
    decimal PreviousProficiencyPercent,
    decimal NewProficiencyPercent,
    bool PreviousIsActive,
    bool NewIsActive,
    DateTime CreatedAt,
    Guid? TriggeredByAttemptId
);
