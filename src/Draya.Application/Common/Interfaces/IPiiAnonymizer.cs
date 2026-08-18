using System;
using System.Threading;
using System.Threading.Tasks;

namespace Draya.Application.Common.Interfaces;

public interface IPiiAnonymizer
{
    /// <summary>
    /// Gets or creates a stable AnonymizedId for the given StudentId.
    /// This AnonymizedId must be used when sending data to external LLMs.
    /// </summary>
    Task<Guid> GetAnonymizedIdAsync(Guid studentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the original StudentId from an AnonymizedId.
    /// Returns null if the AnonymizedId is not found.
    /// </summary>
    Task<Guid?> GetOriginalStudentIdAsync(Guid anonymizedId, CancellationToken cancellationToken = default);
}
