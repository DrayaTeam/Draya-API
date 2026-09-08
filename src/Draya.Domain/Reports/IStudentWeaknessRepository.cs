using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Draya.Domain.Reports;

public interface IStudentWeaknessRepository
{
    Task<StudentWeakness?> GetActiveByTopicAsync(Guid studentId, string topicName, CancellationToken cancellationToken = default);
    Task<List<StudentWeaknessHistory>> GetHistoryAsync(Guid weaknessId, int take = 2, CancellationToken cancellationToken = default);
}
