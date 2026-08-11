namespace Draya.Application.Exams.Services;

public interface IAIExamUsageService
{
    /// <summary>
    /// Checks free quota and wallet balance before AI exam generation.
    /// Returns true if generation is permitted under free quota or paid balance.
    /// Throws InsufficientBalanceException if quota exceeded and balance is insufficient.
    /// </summary>
    Task<bool> ValidateExamGenerationQuotaAsync(Guid teacherId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Records AI exam generation after successful generation.
    /// Increments FreeExamsUsed if under quota, or charges wallet balance (Earned then Purchased)
    /// and increments PaidExamsGenerated.
    /// </summary>
    Task RecordSuccessfulExamGenerationAsync(Guid teacherId, Guid? examId = null, CancellationToken cancellationToken = default);
}
