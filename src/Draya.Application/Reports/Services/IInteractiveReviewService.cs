using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Draya.Application.Reports.Services;

public record TopicRevisionDto(string Recommendation, List<string> SourceMaterials);
public record PracticeExamRequest(Guid SubjectId);

public interface IInteractiveReviewService
{
    Task<TopicRevisionDto> GetRevisionAsync(Guid studentId, string topicName, CancellationToken cancellationToken = default);
    Task<Guid> GeneratePracticeExamAsync(Guid studentId, string topicName, PracticeExamRequest request, CancellationToken cancellationToken = default);
}
