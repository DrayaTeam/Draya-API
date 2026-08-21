using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Draya.Application.Common.Interfaces;
using Draya.Application.Reports.Events;
using Draya.Infrastructure.Persistence;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Draya.Api.Notifications;

public class ReportsNotificationHandlers : 
    INotificationHandler<ReportGeneratedEvent>,
    INotificationHandler<ReportApprovedEvent>,
    INotificationHandler<StudentAtRiskEvent>
{
    private readonly IHubContext<ReportsNotificationHub> _hubContext;
    private readonly ApplicationDbContext _dbContext;
    private readonly IEmailService _emailService;
    private readonly ILogger<ReportsNotificationHandlers> _logger;

    public ReportsNotificationHandlers(
        IHubContext<ReportsNotificationHub> hubContext,
        ApplicationDbContext dbContext,
        IEmailService emailService,
        ILogger<ReportsNotificationHandlers> logger)
    {
        _hubContext = hubContext;
        _dbContext = dbContext;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task Handle(ReportGeneratedEvent notification, CancellationToken cancellationToken)
    {
        var teacherIds = await GetTeachersForStudentAsync(notification.StudentId, cancellationToken);
        
        foreach (var teacherId in teacherIds)
        {
            await _hubContext.Clients.User(teacherId.ToString())
                .SendAsync("ReportGenerated", new { notification.ReportId, notification.StudentId }, cancellationToken);
        }
    }

    public async Task Handle(StudentAtRiskEvent notification, CancellationToken cancellationToken)
    {
        var teacherIds = await GetTeachersForStudentAsync(notification.StudentId, cancellationToken);
        
        foreach (var teacherId in teacherIds)
        {
            await _hubContext.Clients.User(teacherId.ToString())
                .SendAsync("StudentAtRisk", new { notification.StudentId, notification.TopicName }, cancellationToken);
        }
    }

    public async Task Handle(ReportApprovedEvent notification, CancellationToken cancellationToken)
    {
        var student = await _dbContext.Students.FirstOrDefaultAsync(s => s.UserId == notification.StudentId, cancellationToken);
        if (student == null || string.IsNullOrWhiteSpace(student.ParentGuardianEmail))
        {
            _logger.LogWarning("Cannot send email for Report {ReportId} - Parent email is missing for Student {StudentId}", notification.ReportId, notification.StudentId);
            return;
        }

        var report = await _dbContext.PerformanceReports.FirstOrDefaultAsync(r => r.Id == notification.ReportId, cancellationToken);
        if (report == null) return;

        string subject = $"Performance Report for {student.FullName}";
        string body = $"<p>Dear {student.ParentGuardianName},</p>" +
                      $"<p>A new performance report is available for {student.FullName}.</p>" +
                      $"<p><strong>Summary:</strong> {report.SummaryText}</p>" +
                      $"<p>Please log in to the portal for full details.</p>";

        await _emailService.SendEmailAsync(student.ParentGuardianEmail, subject, body, cancellationToken);
    }

    private async Task<System.Collections.Generic.List<Guid>> GetTeachersForStudentAsync(Guid studentId, CancellationToken cancellationToken)
    {
        return await _dbContext.Enrollments
            .Where(e => e.StudentId == studentId && e.Status == Draya.Domain.Classrooms.EnrollmentStatus.Active)
            .Join(_dbContext.Classrooms, e => e.ClassroomId, c => c.Id, (e, c) => c.TeacherId)
            .Distinct()
            .ToListAsync(cancellationToken);
    }
}
