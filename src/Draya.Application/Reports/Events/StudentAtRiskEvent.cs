using System;
using MediatR;

namespace Draya.Application.Reports.Events;

public record StudentAtRiskEvent(Guid StudentId, string TopicName) : INotification;
