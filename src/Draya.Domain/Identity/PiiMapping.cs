using System;

namespace Draya.Domain.Identity;

public class PiiMapping
{
    public Guid StudentId { get; private set; }
    public Guid AnonymizedId { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private PiiMapping() { }

    public PiiMapping(Guid studentId, Guid anonymizedId)
    {
        StudentId = studentId;
        AnonymizedId = anonymizedId;
        CreatedAt = DateTime.UtcNow;
    }
}
