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
        
        // Build the HTML email using the specified brand color #1b6d63
        var html = new System.Text.StringBuilder();
        html.Append($@"
        <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; border: 1px solid #e0e0e0; border-radius: 8px; overflow: hidden; color: #333;'>
            <div style='background-color: #1b6d63; color: white; padding: 20px; text-align: center;'>
                <h1 style='margin: 0; font-size: 24px;'>Performance Report</h1>
                <p style='margin: 5px 0 0 0; font-size: 16px;'>{student.FullName}</p>
            </div>
            
            <div style='padding: 20px;'>
                <p>Dear {student.ParentGuardianName},</p>
                <p>A new performance report has been generated for <strong>{student.FullName}</strong>.</p>
                
                <h2 style='color: #1b6d63; border-bottom: 2px solid #1b6d63; padding-bottom: 5px;'>Overview</h2>
                <div style='background-color: #f9f9f9; padding: 15px; border-radius: 5px; margin-bottom: 20px;'>
                    <ul style='list-style: none; padding: 0; margin: 0;'>
                        <li style='margin-bottom: 8px;'><strong>Classroom Q&A Engagement:</strong> {report.TotalQuestionsReplied} / {report.TotalQuestionsAsked}</li>
                        <li style='margin-bottom: 8px;'><strong>Average Exam Duration:</strong> {report.AverageExamDurationMinutes} minutes</li>
                        <li style='margin-bottom: 8px;'><strong>Completed Lessons:</strong> {report.CompletedLessons}</li>
                        <li><strong>Classroom Percentile:</strong> Top {100 - report.ClassroomPercentile}%</li>
                    </ul>
                </div>

                <h2 style='color: #1b6d63; border-bottom: 2px solid #1b6d63; padding-bottom: 5px;'>AI Teacher Insights</h2>
                <div style='background-color: #e8f4f2; border-left: 4px solid #1b6d63; padding: 15px; margin-bottom: 20px; font-style: italic;'>
                    {report.SummaryText}
                </div>");

        if (report.SubjectProficiencies.Any())
        {
            html.Append(@"
                <h2 style='color: #1b6d63; border-bottom: 2px solid #1b6d63; padding-bottom: 5px;'>Subject Proficiencies</h2>
                <table style='width: 100%; border-collapse: collapse; margin-bottom: 20px;'>
                    <tr>
                        <th style='text-align: left; padding: 8px; border-bottom: 1px solid #ddd;'>Subject</th>
                        <th style='text-align: right; padding: 8px; border-bottom: 1px solid #ddd;'>Score</th>
                    </tr>");
            
            foreach (var sp in report.SubjectProficiencies)
            {
                html.Append($@"
                    <tr>
                        <td style='padding: 8px; border-bottom: 1px solid #eee;'>{sp.SubjectName}</td>
                        <td style='text-align: right; padding: 8px; border-bottom: 1px solid #eee;'>{sp.ProficiencyPercent}%</td>
                    </tr>");
            }
            html.Append("</table>");
        }

        if (report.WeakTopics.Any())
        {
            html.Append(@"
                <h2 style='color: #1b6d63; border-bottom: 2px solid #1b6d63; padding-bottom: 5px;'>Areas for Improvement</h2>
                <div style='margin-bottom: 20px;'>");
            
            foreach (var wt in report.WeakTopics)
            {
                html.Append($@"
                    <div style='margin-bottom: 15px; padding-bottom: 15px; border-bottom: 1px solid #eee;'>
                        <h3 style='margin: 0 0 5px 0; font-size: 16px;'>{wt.TopicName} <span style='font-size: 12px; font-weight: normal; color: #666;'>({wt.SubjectName})</span></h3>
                        <p style='margin: 0 0 5px 0;'><strong>Score:</strong> {wt.ProficiencyPercent}% &nbsp;|&nbsp; <strong>Status:</strong> {wt.Status}</p>
                        <p style='margin: 0; color: #555;'><strong>Recommendation:</strong> {wt.Recommendation}</p>
                    </div>");
            }
            html.Append("</div>");
        }

        html.Append(@"
                <div style='text-align: center; margin-top: 30px;'>
                    <p style='margin-bottom: 15px;'>Please log in to the portal for full details.</p>
                </div>
            </div>
            <div style='background-color: #f4f4f4; padding: 10px; text-align: center; font-size: 12px; color: #777;'>
                &copy; Draya Platform. All rights reserved.
            </div>
        </div>");

        string body = html.ToString();

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
