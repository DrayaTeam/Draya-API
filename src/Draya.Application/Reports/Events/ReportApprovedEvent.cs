using System;
using MediatR;

namespace Draya.Application.Reports.Events;

public record ReportApprovedEvent(Guid ReportId, Guid StudentId) : INotification;
