using System;
using System.Threading;
using System.Threading.Tasks;
using Draya.Application.Reports.Events;
using Draya.Domain.Reports;
using MediatR;

namespace Draya.Application.Reports.Commands;

public record ApproveReportCommand(Guid ReportId) : IRequest<bool>;

public class ApproveReportCommandHandler : IRequestHandler<ApproveReportCommand, bool>
{
    private readonly IPerformanceReportRepository _reportRepository;
    private readonly IMediator _mediator;

    public ApproveReportCommandHandler(IPerformanceReportRepository reportRepository, IMediator mediator)
    {
        _reportRepository = reportRepository;
        _mediator = mediator;
    }

    public async Task<bool> Handle(ApproveReportCommand request, CancellationToken cancellationToken)
    {
        var report = await _reportRepository.GetByIdAsync(request.ReportId, cancellationToken);
        
        if (report == null)
            return false;

        if (report.IsApproved)
            return true; // Already approved

        report.Approve();
        
        await _reportRepository.UpdateAsync(report, cancellationToken);
        
        // Trigger event to send parent email
        await _mediator.Publish(new ReportApprovedEvent(report.Id, report.StudentId), cancellationToken);

        return true;
    }
}
