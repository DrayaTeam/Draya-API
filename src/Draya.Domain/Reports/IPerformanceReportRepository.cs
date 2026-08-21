using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Draya.Domain.Reports;

public interface IPerformanceReportRepository
{
    Task AddAsync(PerformanceReport report, CancellationToken cancellationToken = default);
    Task<PerformanceReport?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PerformanceReport?> GetLatestByStudentIdAsync(Guid studentId, CancellationToken cancellationToken = default);
    Task UpdateAsync(PerformanceReport report, CancellationToken cancellationToken = default);
    IQueryable<PerformanceReport> GetQueryable();
}
