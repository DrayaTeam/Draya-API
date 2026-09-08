using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Draya.Api.Controllers;
using Draya.Api.Tests.Infrastructure;
using Draya.Application.Exams.Commands.Attempts;
using Draya.Domain.Classrooms;
using Draya.Domain.Exams;
using Draya.Domain.Identity;
using Draya.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Draya.Api.Tests.Controllers;

public class ExamAttemptsControllerTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory<Program> _factory;

    public ExamAttemptsControllerTests(CustomWebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task<(Guid studentId, Guid examId, Guid questionId, Guid optionId)> SetupTestDataAsync()
    {
        var studentId = Guid.NewGuid();
        var examId = Guid.NewGuid();
        var questionId = Guid.NewGuid();
        var optionId = Guid.NewGuid();
        var classroomId = Guid.NewGuid();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // Create student using correct constructor/properties
        // In Identity, users are ApplicationUser. Student might be a derived class.
        // Actually Student is in Draya.Domain.Identity. Let's create it properly.
        var student = new Student();
        student.GetType().GetProperty("UserId")?.SetValue(student, studentId);
        student.GetType().GetProperty("FullName")?.SetValue(student, "Test Student");
        db.Students.Add(student);

        var classroom = new Classroom { Id = classroomId, Name = "Test Class", TeacherId = Guid.NewGuid(), SubjectId = Guid.NewGuid(), ClassroomTypeId = Guid.NewGuid(), GradeLevelId = Guid.NewGuid() };
        db.Classrooms.Add(classroom);

        db.Enrollments.Add(new Enrollment
        {
            StudentId = studentId,
            ClassroomId = classroomId,
            Status = EnrollmentStatus.Active
        });

        // Create exam with 1 objective question
        var exam = new Exam(classroomId, Guid.NewGuid(), "API Test Exam", "API Testing", 60, DateTime.UtcNow);
        exam.GetType().GetProperty("Id")?.SetValue(exam, examId);
        
        var question = new ExamQuestion(examId, "API Question", "MultipleChoice", "Easy", "");
        question.GetType().GetProperty("Id")?.SetValue(question, questionId);
        
        var option = new ExamQuestionOption(questionId, "Correct Option", true);
        option.GetType().GetProperty("Id")?.SetValue(option, optionId);
        
        question.AddOption(option);
        exam.AddQuestion(question);
        
        db.Exams.Add(exam);
        await db.SaveChangesAsync();

        return (studentId, examId, questionId, optionId);
    }

    [Fact]
    public async Task StartAttempt_ReturnsOk_WhenStudentEnrolled()
    {
        // Arrange
        var (studentId, examId, _, _) = await SetupTestDataAsync();
        
        // Mock authentication
        _client.DefaultRequestHeaders.Add("X-Test-UserId", studentId.ToString());
        _client.DefaultRequestHeaders.Add("X-Test-Role", "Student");

        var request = new StartAttemptRequestDto { ExamId = examId };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/attempts/start", request);

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        Assert.True(result.TryGetProperty("attemptId", out var attemptIdProp));
        Assert.True(Guid.TryParse(attemptIdProp.GetString(), out _));
    }

    [Fact]
    public async Task SubmitAttempt_GradesAndReturnsOk_ForObjectiveExam()
    {
        // Arrange
        var (studentId, examId, questionId, optionId) = await SetupTestDataAsync();
        
        _client.DefaultRequestHeaders.Add("X-Test-UserId", studentId.ToString());
        _client.DefaultRequestHeaders.Add("X-Test-Role", "Student");

        // Start the attempt
        var startResponse = await _client.PostAsJsonAsync("/api/v1/attempts/start", new StartAttemptRequestDto { ExamId = examId });
        var startResult = await startResponse.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        var attemptId = startResult.GetProperty("attemptId").GetString();

        // Submit the attempt
        var submitRequest = new SubmitAttemptRequestDto
        {
            IdempotencyKey = Guid.NewGuid().ToString(),
            Answers = new List<AnswerSubmissionDto>
            {
                new AnswerSubmissionDto(questionId, null, optionId)
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync($"/api/v1/attempts/{attemptId}/submit", submitRequest);

        // Assert
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new Exception($"Submission failed: {response.StatusCode} - {errorContent}");
        }
        
        var submitResult = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        Assert.Equal("Exam submitted and auto-graded successfully.", submitResult.GetProperty("message").GetString());
        
        // Act: Get Results (Bug 8 fix verification)
        var resultsResponse = await _client.GetAsync($"/api/v1/attempts/{attemptId}/results");
        resultsResponse.EnsureSuccessStatusCode();
        
        var results = await resultsResponse.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        Assert.True(results.GetProperty("isSubmitted").GetBoolean());
        Assert.Equal(1.0m, results.GetProperty("finalScore").GetDecimal()); // The correct option gives 1.0m score
    }
}
