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

    public async Task<StudentAnalyticsDto> GetAnalyticsAsync(Guid studentId, CancellationToken cancellationToken = default)
    {
        // Get all completed attempts for the student
        var completedAttemptsQuery = _dbContext.StudentExamAttempts
            .Where(a => a.StudentId == studentId && a.IsSubmitted && a.FinalScore != null);

        var completedExamsCount = await completedAttemptsQuery.CountAsync(cancellationToken);
        
        decimal overallAverage = 0m;
        decimal highestScore = 0m;

        if (completedExamsCount > 0)
        {
            overallAverage = await completedAttemptsQuery.AverageAsync(a => a.FinalScore!.Value, cancellationToken);
            highestScore = await completedAttemptsQuery.MaxAsync(a => a.FinalScore!.Value, cancellationToken);
        }

        // Fetch proficiency per subject and topic
        // A correct answer has GradingResult.Score == 1.0 (or > 0, depending on subjective). We'll compute percentage of earned score / max possible score (assuming max is 1 per question).
        var answersData = await _dbContext.StudentExamAttempts
            .Where(attempt => attempt.StudentId == studentId && attempt.IsSubmitted)
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
                string status = avgScore < 50m ? "Needs urgent improvement" : "Improving";
                
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

        return new StudentAnalyticsDto(
            overallAverage,
            highestScore,
            completedExamsCount,
            subjectProficiencies,
            trendPoints,
            weakTopics
        );
    }
}
