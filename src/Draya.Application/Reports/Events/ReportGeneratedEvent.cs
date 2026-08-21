using System;
using MediatR;

namespace Draya.Application.Reports.Events;

public class ReportGeneratedEvent : INotification
{
    public Guid ReportId { get; }
    public Guid StudentId { get; }

    public ReportGeneratedEvent(Guid reportId, Guid studentId)
    {
        ReportId = reportId;
        StudentId = studentId;
    }
}
