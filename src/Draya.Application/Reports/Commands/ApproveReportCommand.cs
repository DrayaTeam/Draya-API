using System;
using System.Threading;
using System.Threading.Tasks;
using Draya.Application.Reports.Events;
using Draya.Domain.Reports;
using MediatR;

namespace Draya.Application.Reports.Commands;

public enum ApproveReportResult
{
    Success,
    NotFound,
    AlreadyApproved
}

public record ApproveReportCommand(Guid ReportId) : IRequest<ApproveReportResult>;

public class ApproveReportCommandHandler : IRequestHandler<ApproveReportCommand, ApproveReportResult>
{
    private readonly IPerformanceReportRepository _reportRepository;
    private readonly IMediator _mediator;

    public ApproveReportCommandHandler(IPerformanceReportRepository reportRepository, IMediator mediator)
    {
        _reportRepository = reportRepository;
        _mediator = mediator;
    }

    public async Task<ApproveReportResult> Handle(ApproveReportCommand request, CancellationToken cancellationToken)
    {
        var report = await _reportRepository.GetByIdAsync(request.ReportId, cancellationToken);
        
        if (report == null)
            return ApproveReportResult.NotFound;

        if (report.IsApproved)
            return ApproveReportResult.AlreadyApproved;

        report.Approve();
        
        await _reportRepository.UpdateAsync(report, cancellationToken);
        
        // Trigger event to send parent email
        await _mediator.Publish(new ReportApprovedEvent(report.Id, report.StudentId), cancellationToken);

        return ApproveReportResult.Success;
    }
}
