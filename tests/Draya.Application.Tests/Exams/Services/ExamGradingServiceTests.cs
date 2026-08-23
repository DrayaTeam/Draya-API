using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Draya.Application.AI;
using Draya.Application.AI.Models;
using Draya.Application.Common.Interfaces;
using Draya.Application.Exams.Services;
using Draya.Domain.Exams;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Draya.Application.Tests.Exams.Services;

public class ExamGradingServiceTests
{
    private readonly Mock<IExamGradingJobRepository> _jobRepoMock = new();
    private readonly Mock<IStudentExamAttemptRepository> _attemptRepoMock = new();
    private readonly Mock<IExamRepository> _examRepoMock = new();
    private readonly Mock<IExamGradingTaskQueue> _taskQueueMock = new();
    private readonly Mock<ILLMService> _llmServiceMock = new();
    private readonly Mock<IPiiAnonymizer> _piiAnonymizerMock = new();
    private readonly Mock<ILogger<ExamGradingService>> _loggerMock = new();
    private readonly Mock<IPublisher> _publisherMock = new();
    private readonly Mock<MediatR.ISender> _senderMock = new();

    private readonly ExamGradingService _sut;

    public ExamGradingServiceTests()
    {
        _sut = new ExamGradingService(
            _jobRepoMock.Object,
            _attemptRepoMock.Object,
            _examRepoMock.Object,
            _taskQueueMock.Object,
            _llmServiceMock.Object,
            _piiAnonymizerMock.Object,
            _loggerMock.Object,
            _publisherMock.Object,
            _senderMock.Object);
    }

    [Fact]
    public async Task StartGradingAsync_ExistingJob_ReturnsExistingId()
    {
        var idempotencyKey = "test-key";
        var existingJob = new ExamGradingJob(Guid.NewGuid(), idempotencyKey);
        
        _jobRepoMock.Setup(x => x.GetByIdempotencyKeyAsync(idempotencyKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingJob);

        var result = await _sut.StartGradingAsync(new StartGradingRequest { StudentExamAttemptId = Guid.NewGuid(), IdempotencyKey = idempotencyKey });

        Assert.Equal(existingJob.Id, result);
        _jobRepoMock.Verify(x => x.AddAsync(It.IsAny<ExamGradingJob>(), It.IsAny<CancellationToken>()), Times.Never);
        _taskQueueMock.Verify(x => x.QueueBackgroundWorkItemAsync(It.IsAny<ExamGradingItem>()), Times.Never);
    }

    [Fact]
    public async Task StartGradingAsync_NewJob_CreatesAndQueuesJob()
    {
        var idempotencyKey = "test-key";
        var attemptId = Guid.NewGuid();
        
        _jobRepoMock.Setup(x => x.GetByIdempotencyKeyAsync(idempotencyKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ExamGradingJob?)null);

        var result = await _sut.StartGradingAsync(new StartGradingRequest { StudentExamAttemptId = attemptId, IdempotencyKey = idempotencyKey });

        Assert.NotEqual(Guid.Empty, result);
        _jobRepoMock.Verify(x => x.AddAsync(It.Is<ExamGradingJob>(j => j.Id == result && j.IdempotencyKey == idempotencyKey), It.IsAny<CancellationToken>()), Times.Once);
        _taskQueueMock.Verify(x => x.QueueBackgroundWorkItemAsync(It.Is<ExamGradingItem>(i => i.GradingJobId == result && i.StudentExamAttemptId == attemptId)), Times.Once);
    }

    [Fact]
    public async Task ProcessGradingAsync_JobNotFound_ReturnsEarly()
    {
        var jobId = Guid.NewGuid();
        _jobRepoMock.Setup(x => x.GetByIdAsync(jobId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ExamGradingJob?)null);

        await _sut.ProcessGradingAsync(jobId, Guid.NewGuid());

        _jobRepoMock.Verify(x => x.UpdateAsync(It.IsAny<ExamGradingJob>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ProcessGradingAsync_AttemptNotFound_FailsJob()
    {
        var jobId = Guid.NewGuid();
        var job = new ExamGradingJob(Guid.NewGuid(), "key");
        _jobRepoMock.Setup(x => x.GetByIdAsync(jobId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);
        
        _attemptRepoMock.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((StudentExamAttempt?)null);

        await _sut.ProcessGradingAsync(jobId, Guid.NewGuid());

        Assert.Equal(GradingStatus.Failed, job.Status);
        Assert.Equal("Exam attempt not found", job.ErrorMessage);
        _jobRepoMock.Verify(x => x.UpdateAsync(job, It.IsAny<CancellationToken>()), Times.Exactly(2)); // 1 for Grading, 1 for Failed
    }

    [Fact]
    public async Task ProcessGradingAsync_ObjectiveQuestion_MultipleChoiceCorrect_ScoresCorrectly()
    {
        var (jobId, job, attempt, exam) = SetupGradingContext();
        
        var question = new ExamQuestion(exam.Id, "Q1", "MultipleChoice", "Easy", "", "");
        var correctOption = new ExamQuestionOption(question.Id, "A", true);
        question.AddOption(correctOption);
        exam.AddQuestion(question);

        var answer = new StudentAnswer(attempt.Id, question.Id, "", correctOption.Id);
        attempt.AddAnswer(answer);

        await _sut.ProcessGradingAsync(jobId, attempt.Id);

        Assert.Equal(GradingStatus.Completed, job.Status);
        Assert.NotNull(answer.GradingResult);
        Assert.Equal(1.0m, answer.GradingResult.Score);
        Assert.False(answer.GradingResult.NeedsTeacherReview);
        Assert.False(answer.GradingResult.IsAiGraded);
    }

    [Fact]
    public async Task ProcessGradingAsync_ObjectiveQuestion_MultipleChoiceIncorrect_ScoresZero()
    {
        var (jobId, job, attempt, exam) = SetupGradingContext();
        
        var question = new ExamQuestion(exam.Id, "Q1", "MultipleChoice", "Easy", "", "");
        var correctOption = new ExamQuestionOption(question.Id, "A", true);
        var wrongOption = new ExamQuestionOption(question.Id, "B", false);
        question.AddOption(correctOption);
        question.AddOption(wrongOption);
        exam.AddQuestion(question);

        var answer = new StudentAnswer(attempt.Id, question.Id, "", wrongOption.Id);
        attempt.AddAnswer(answer);

        await _sut.ProcessGradingAsync(jobId, attempt.Id);

        Assert.NotNull(answer.GradingResult);
        Assert.Equal(0m, answer.GradingResult.Score);
    }

    [Fact]
    public async Task ProcessGradingAsync_ObjectiveQuestion_FillInTheBlankCorrect_CaseInsensitive()
    {
        var (jobId, job, attempt, exam) = SetupGradingContext();
        
        var question = new ExamQuestion(exam.Id, "Q1", "FillInTheBlank", "Easy", "", "");
        question.AddOption(new ExamQuestionOption(question.Id, "Paris", true));
        exam.AddQuestion(question);

        var answer = new StudentAnswer(attempt.Id, question.Id, " pARIS  "); // Test case insensitivity and trimming, wait trimming needs to be on exact, service uses: o.Text.Equals(answer.AnswerText?.Trim(), StringComparison.OrdinalIgnoreCase)
        attempt.AddAnswer(answer);

        await _sut.ProcessGradingAsync(jobId, attempt.Id);

        Assert.NotNull(answer.GradingResult);
        Assert.Equal(1.0m, answer.GradingResult.Score);
    }

    [Fact]
    public async Task ProcessGradingAsync_SubjectiveQuestion_HighConfidence_NoTeacherReview()
    {
        var (jobId, job, attempt, exam) = SetupGradingContext();
        
        var question = new ExamQuestion(exam.Id, "Q1", "Essay", "Medium", "", "Rubric");
        exam.AddQuestion(question);

        var answer = new StudentAnswer(attempt.Id, question.Id, "A good essay.");
        attempt.AddAnswer(answer);

        // Mock LLM successful high confidence
        SetupLlmResponse(new { score = 1.0, confidenceScore = 0.9, rationale = "Good" });

        await _sut.ProcessGradingAsync(jobId, attempt.Id);

        Assert.Equal(GradingStatus.Completed, job.Status);
        Assert.NotNull(answer.GradingResult);
        Assert.Equal(1.0m, answer.GradingResult.Score);
        Assert.Equal(0.9m, answer.GradingResult.ConfidenceScore);
        Assert.False(answer.GradingResult.NeedsTeacherReview);
        Assert.True(answer.GradingResult.IsAiGraded);
    }

    [Fact]
    public async Task ProcessGradingAsync_SubjectiveQuestion_LowConfidence_RequiresTeacherReview()
    {
        var (jobId, job, attempt, exam) = SetupGradingContext();
        
        var question = new ExamQuestion(exam.Id, "Q1", "Essay", "Medium", "", "Rubric");
        exam.AddQuestion(question);

        var answer = new StudentAnswer(attempt.Id, question.Id, "A mediocre essay.");
        attempt.AddAnswer(answer);

        // Mock LLM successful low confidence (< 0.85)
        SetupLlmResponse(new { score = 0.5, confidenceScore = 0.8, rationale = "Not sure" });

        await _sut.ProcessGradingAsync(jobId, attempt.Id);

        Assert.Equal(GradingStatus.CompletedWithWarning, job.Status); // Job has warnings
        Assert.NotNull(answer.GradingResult);
        Assert.Equal(0.5m, answer.GradingResult.Score);
        Assert.True(answer.GradingResult.NeedsTeacherReview);
    }

    [Fact]
    public async Task ProcessGradingAsync_SubjectiveQuestion_EmptyAnswer_ScoresZeroWithReview()
    {
        var (jobId, job, attempt, exam) = SetupGradingContext();
        
        var question = new ExamQuestion(exam.Id, "Q1", "Essay", "Medium", "", "Rubric");
        exam.AddQuestion(question);

        var answer = new StudentAnswer(attempt.Id, question.Id, ""); // Empty answer
        attempt.AddAnswer(answer);

        // Mock LLM returning 0 for empty answer
        SetupLlmResponse(new { score = 0.0, confidenceScore = 0.95, rationale = "Answer is empty." });

        await _sut.ProcessGradingAsync(jobId, attempt.Id);

        Assert.NotNull(answer.GradingResult);
        Assert.Equal(0.0m, answer.GradingResult.Score);
        // Wait, if confidence > 0.85, it doesn't need review.
        Assert.False(answer.GradingResult.NeedsTeacherReview);
    }

    [Fact]
    public async Task ProcessGradingAsync_SubjectiveQuestion_LlmThrowsException_DefaultsToReviewAndZero()
    {
        var (jobId, job, attempt, exam) = SetupGradingContext();
        
        var question = new ExamQuestion(exam.Id, "Q1", "Essay", "Medium", "", "Rubric");
        exam.AddQuestion(question);

        var answer = new StudentAnswer(attempt.Id, question.Id, "A good essay.");
        attempt.AddAnswer(answer);

        _llmServiceMock.Setup(x => x.GenerateAsync(It.IsAny<LlmRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("LLM is down"));

        await _sut.ProcessGradingAsync(jobId, attempt.Id);

        Assert.NotNull(answer.GradingResult);
        Assert.Equal(0m, answer.GradingResult.Score);
        Assert.True(answer.GradingResult.NeedsTeacherReview);
        Assert.Equal("AI Grading failed", answer.GradingResult.Rationale);
        Assert.Equal(GradingStatus.CompletedWithWarning, job.Status);
    }

    [Fact]
    public async Task ProcessGradingAsync_SubjectiveQuestion_MalformedJsonResponse_DefaultsToReviewAndZero()
    {
        var (jobId, job, attempt, exam) = SetupGradingContext();
        
        var question = new ExamQuestion(exam.Id, "Q1", "Essay", "Medium", "", "Rubric");
        exam.AddQuestion(question);

        var answer = new StudentAnswer(attempt.Id, question.Id, "A good essay.");
        attempt.AddAnswer(answer);

        _llmServiceMock.Setup(x => x.GenerateAsync(It.IsAny<LlmRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LlmResponse { Content = "I'm sorry, I can't do that." }); // Not JSON

        await _sut.ProcessGradingAsync(jobId, attempt.Id);

        Assert.NotNull(answer.GradingResult);
        Assert.Equal(0m, answer.GradingResult.Score);
        Assert.True(answer.GradingResult.NeedsTeacherReview);
        Assert.Equal("AI Grading failed", answer.GradingResult.Rationale);
    }
    
    [Fact]
    public async Task ProcessGradingAsync_SubjectiveQuestion_JsonWithMarkdownBlock_CleansAndParses()
    {
        var (jobId, job, attempt, exam) = SetupGradingContext();
        
        var question = new ExamQuestion(exam.Id, "Q1", "Essay", "Medium", "", "Rubric");
        exam.AddQuestion(question);

        var answer = new StudentAnswer(attempt.Id, question.Id, "Test.");
        attempt.AddAnswer(answer);

        var jsonString = JsonSerializer.Serialize(new { score = 1.0, confidenceScore = 0.9, rationale = "Good" });
        _llmServiceMock.Setup(x => x.GenerateAsync(It.IsAny<LlmRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LlmResponse { Content = $"```json\n{jsonString}\n```" });

        await _sut.ProcessGradingAsync(jobId, attempt.Id);

        Assert.NotNull(answer.GradingResult);
        Assert.Equal(1.0m, answer.GradingResult.Score);
        Assert.False(answer.GradingResult.NeedsTeacherReview);
    }

    private (Guid, ExamGradingJob, StudentExamAttempt, Exam) SetupGradingContext()
    {
        var studentId = Guid.NewGuid();

        var job = new ExamGradingJob(Guid.NewGuid(), "key");
        var jobId = job.Id;

        var exam = new Exam(Guid.NewGuid(), Guid.NewGuid(), "Test Exam", "Topic", 60, DateTime.UtcNow, DateTime.UtcNow.AddDays(1), 1);
        var examId = exam.Id;

        var attempt = new StudentExamAttempt(examId, studentId);
        var attemptId = attempt.Id;

        _jobRepoMock.Setup(x => x.GetByIdAsync(jobId, It.IsAny<CancellationToken>())).ReturnsAsync(job);
        _attemptRepoMock.Setup(x => x.GetByIdAsync(attemptId, It.IsAny<CancellationToken>())).ReturnsAsync(attempt);
        _examRepoMock.Setup(x => x.GetByIdAsync(examId, It.IsAny<CancellationToken>())).ReturnsAsync(exam);
        _piiAnonymizerMock.Setup(x => x.GetAnonymizedIdAsync(studentId, It.IsAny<CancellationToken>())).ReturnsAsync(Guid.NewGuid());

        return (jobId, job, attempt, exam);
    }

    private void SetupLlmResponse(object responseObj)
    {
        var json = JsonSerializer.Serialize(responseObj);
        _llmServiceMock.Setup(x => x.GenerateAsync(It.IsAny<LlmRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LlmResponse { Content = json });
    }
}
