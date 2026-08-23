using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Draya.Application.Reports.Services;
using Draya.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Draya.Infrastructure.Reports.Services;

public class StudentAnalyticsService : IStudentAnalyticsService
{
    private readonly ApplicationDbContext _dbContext;

    public StudentAnalyticsService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<StudentAnalyticsDto> GetAnalyticsAsync(Guid studentId, Guid? teacherId = null, CancellationToken cancellationToken = default)
    {
        IQueryable<Guid>? teacherClassroomIds = null;
        if (teacherId.HasValue)
        {
            teacherClassroomIds = _dbContext.Classrooms.Where(c => c.TeacherId == teacherId.Value).Select(c => c.Id);
        }

        // Get all completed attempts for the student
        var completedAttemptsQuery = _dbContext.StudentExamAttempts
            .Where(a => a.StudentId == studentId && a.IsSubmitted && a.FinalScore != null);

        if (teacherId.HasValue)
        {
            completedAttemptsQuery = completedAttemptsQuery
                .Join(_dbContext.Exams, a => a.ExamId, e => e.Id, (a, e) => new { Attempt = a, Exam = e })
                .Where(x => teacherClassroomIds!.Contains(x.Exam.ClassroomId))
                .Select(x => x.Attempt);
        }

        var completedExamsCount = await completedAttemptsQuery.CountAsync(cancellationToken);
        
        decimal overallAverage = 0m;
        decimal highestScore = 0m;

        if (completedExamsCount > 0)
        {
            overallAverage = await completedAttemptsQuery.AverageAsync(a => (decimal?)a.FinalScore) ?? 0m;
            highestScore = await completedAttemptsQuery.MaxAsync(a => (decimal?)a.FinalScore) ?? 0m;
        }

        // Fetch proficiency per subject and topic
        // A correct answer has GradingResult.Score == 1.0 (or > 0, depending on subjective). We'll compute percentage of earned score / max possible score (assuming max is 1 per question).
        var baseAnswersQuery = _dbContext.StudentExamAttempts
            .Where(attempt => attempt.StudentId == studentId && attempt.IsSubmitted);

        if (teacherId.HasValue)
        {
            baseAnswersQuery = baseAnswersQuery
                .Join(_dbContext.Exams, a => a.ExamId, e => e.Id, (a, e) => new { Attempt = a, Exam = e })
                .Where(x => teacherClassroomIds!.Contains(x.Exam.ClassroomId))
                .Select(x => x.Attempt);
        }

        var answersData = await baseAnswersQuery
            .Join(_dbContext.StudentAnswers.Include(a => a.GradingResult), attempt => attempt.Id, answer => answer.StudentExamAttemptId, (attempt, answer) => new { Attempt = attempt, Answer = answer })
            .Where(x => x.Answer.GradingResult != null)
            .Join(_dbContext.ExamQuestions, x => x.Answer.ExamQuestionId, q => q.Id, (x, q) => new { x.Attempt, x.Answer, Question = q })
            .Join(_dbContext.Exams, x => x.Attempt.ExamId, e => e.Id, (x, e) => new { x.Attempt, x.Answer, x.Question, Exam = e })
            .Join(_dbContext.Classrooms, x => x.Exam.ClassroomId, c => c.Id, (x, c) => new { x.Attempt, x.Answer, x.Question, x.Exam, Classroom = c })
            .Join(_dbContext.Subjects, x => x.Classroom.SubjectId, s => s.Id, (x, s) => new { x.Attempt, x.Answer, x.Question, x.Exam, x.Classroom, Subject = s })
            .Select(x => new 
            { 
                SubjectName = x.Subject.Name, 
                TopicName = x.Exam.Topic, 
                Score = x.Answer.GradingResult!.Score,
                QuestionText = x.Question.Text,
                AnswerText = x.Answer.AnswerText
            })
            .ToListAsync(cancellationToken);

        // Compute Subject Proficiencies
        var subjectProficiencies = answersData
            .GroupBy(x => x.SubjectName)
            .Select(g => new SubjectProficiencyResult(g.Key, g.Count() > 0 ? (g.Average(x => x.Score) * 100) : 0))
            .ToList();

        // Compute Topic Proficiencies and find Weak Topics
        var weakTopics = new List<WeakTopicResult>();
        var topicGroups = answersData.GroupBy(x => new { x.SubjectName, x.TopicName });
        
        foreach (var group in topicGroups)
        {
            var avgScore = group.Count() > 0 ? (group.Average(x => x.Score) * 100) : 0;
            if (avgScore < 75m)
            {
                string status = avgScore < 50m ? Draya.Domain.Reports.ProficiencyStatus.NeedsUrgentImprovement : Draya.Domain.Reports.ProficiencyStatus.Improving;
                
                // Get some incorrect answers for the AI
                var incorrectAnswers = group
                    .Where(x => x.Score < 1.0m && !string.IsNullOrWhiteSpace(x.AnswerText))
                    .Select(x => $"Q: {x.QuestionText} | A: {x.AnswerText}")
                    .Take(3)
                    .ToList();

                weakTopics.Add(new WeakTopicResult(group.Key.TopicName, group.Key.SubjectName, avgScore, status, incorrectAnswers));
            }
        }

        // Compute Month-over-Month Trends (from exam attempts)
        var trendData = await completedAttemptsQuery
            .Select(a => new { a.SubmittedAt!.Value.Year, a.SubmittedAt.Value.Month, Score = a.FinalScore!.Value })
            .ToListAsync(cancellationToken);

        var trendPoints = trendData
            .GroupBy(x => new { x.Year, x.Month })
            .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
            .Select(g => new TrendPointResult(new DateTime(g.Key.Year, g.Key.Month, 1), g.Average(x => x.Score)))
            .ToList();

        // 1. Student Engagement
        var totalQuestionsAsked = await _dbContext.Questions.CountAsync(q => q.AuthorId == studentId, cancellationToken);
        var totalQuestionsReplied = await _dbContext.QuestionReplies.CountAsync(r => r.AuthorId == studentId, cancellationToken);

        // 2. Exam Speed
        var avgDurationMinutes = 0m;
        var durationData = await completedAttemptsQuery
            .Select(a => new { a.StartedAt, a.SubmittedAt })
            .ToListAsync(cancellationToken);
            
        var durations = durationData
            .Where(a => a.SubmittedAt.HasValue)
            .Select(a => (decimal)(a.SubmittedAt!.Value - a.StartedAt).TotalMinutes)
            .ToList();
            
        if (durations.Any())
        {
            avgDurationMinutes = durations.Average();
        }

        // 3. Material Consumption (using CompletedLessons)
        var completedLessons = await _dbContext.Enrollments
            .Where(e => e.StudentId == studentId && e.Status == Draya.Domain.Classrooms.EnrollmentStatus.Active)
            .SumAsync(e => e.CompletedLessons, cancellationToken);

        // 4. Peer Comparison
        decimal classroomPercentile = 0m;
        var classroomIds = await _dbContext.Enrollments
            .Where(e => e.StudentId == studentId)
            .Select(e => e.ClassroomId)
            .ToListAsync(cancellationToken);
            
        if (classroomIds.Any() && overallAverage > 0)
        {
            var peerScores = await _dbContext.StudentExamAttempts
                .Join(_dbContext.Exams, a => a.ExamId, e => e.Id, (a, e) => new { Attempt = a, Exam = e })
                .Where(x => classroomIds.Contains(x.Exam.ClassroomId) && x.Attempt.IsSubmitted && x.Attempt.FinalScore != null)
                .Select(x => x.Attempt.FinalScore!.Value)
                .ToListAsync(cancellationToken);

            if (peerScores.Any())
            {
                var lowerScoresCount = peerScores.Count(s => s < overallAverage);
                classroomPercentile = Math.Round((decimal)lowerScoresCount / peerScores.Count * 100m, 2);
            }
        }

        return new StudentAnalyticsDto(
            overallAverage,
            highestScore,
            completedExamsCount,
            subjectProficiencies,
            trendPoints,
            weakTopics,
            totalQuestionsAsked,
            totalQuestionsReplied,
            avgDurationMinutes,
            completedLessons,
            classroomPercentile
        );
    }
}
