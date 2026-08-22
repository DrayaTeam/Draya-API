using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Draya.Application.Exams.Commands.Attempts;
using Draya.Application.Exams.Services;
using Draya.Domain.Exams;
using Draya.Domain.Exams.Exceptions;
using Moq;
using Xunit;

namespace Draya.Application.Tests.Exams.Commands.Attempts;

public class SubmitExamAttemptCommandHandlerTests
{
    private readonly Mock<IStudentExamAttemptRepository> _attemptRepoMock;
    private readonly Mock<IExamGradingService> _gradingServiceMock;
    private readonly Mock<IExamRepository> _examRepoMock;
    private readonly SubmitExamAttemptCommandHandler _handler;

    public SubmitExamAttemptCommandHandlerTests()
    {
        _attemptRepoMock = new Mock<IStudentExamAttemptRepository>();
        _gradingServiceMock = new Mock<IExamGradingService>();
        _examRepoMock = new Mock<IExamRepository>();

        _handler = new SubmitExamAttemptCommandHandler(
            _attemptRepoMock.Object,
            _gradingServiceMock.Object,
            _examRepoMock.Object
        );
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenAnswerMissingBothTextAndOption()
    {
        // Arrange
        var command = new SubmitExamAttemptCommand(
            Guid.NewGuid(),
            new List<AnswerSubmissionDto>
            {
                new AnswerSubmissionDto(Guid.NewGuid(), "   ", null)
            },
            "idempotency-key"
        );

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => _handler.Handle(command, CancellationToken.None));
        Assert.Contains("Either SelectedOptionId or AnswerText must be provided", ex.Message);
    }

    [Fact]
    public async Task Handle_ShouldThrow_IfAttemptAlreadySubmitted()
    {
        // Arrange
        var attemptId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var examId = Guid.NewGuid();
        
        var attempt = new StudentExamAttempt(examId, studentId);
        attempt.Submit(); // Mark as submitted

        _attemptRepoMock.Setup(x => x.GetByIdAsync(attemptId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(attempt);

        var command = new SubmitExamAttemptCommand(
            attemptId,
            new List<AnswerSubmissionDto>
            {
                new AnswerSubmissionDto(Guid.NewGuid(), "Answer", null)
            },
            "idempotency-key"
        );

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ExamAttemptSubmissionException>(() => _handler.Handle(command, CancellationToken.None));
        Assert.Equal("Attempt has already been submitted", ex.Message);
    }

    [Fact]
    public async Task Handle_ShouldGradeMCQsImmediately()
    {
        // Arrange
        var attemptId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var exam = new Exam(Guid.NewGuid(), Guid.NewGuid(), "Test Exam", "Test Topic", 60, DateTime.UtcNow);
        var examId = exam.Id;
        var attempt = new StudentExamAttempt(examId, studentId);
        
        var questionId = Guid.NewGuid();
        var correctOptionId = Guid.NewGuid();
        var question = new ExamQuestion(exam.Id, "What is 2+2?", "MultipleChoice", "Easy", "");
        question.GetType().GetProperty("Id")?.SetValue(question, questionId);
        
        question.AddOption(new ExamQuestionOption(question.Id, "4", true));
        // Force the correct option ID
        var option = question.Options.First();
        option.GetType().GetProperty("Id")?.SetValue(option, correctOptionId);
        
        exam.AddQuestion(question);

        _attemptRepoMock.Setup(x => x.GetByIdAsync(attempt.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(attempt);
            
        _examRepoMock.Setup(x => x.GetByIdAsync(exam.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(exam);

        var command = new SubmitExamAttemptCommand(
            attempt.Id,
            new List<AnswerSubmissionDto>
            {
                new AnswerSubmissionDto(questionId, null, correctOptionId)
            },
            "idempotency-key"
        );

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Null(result); // Null because there are no subjective questions, so no background job is triggered
        
        // Verify that the attempt was marked submitted and saved
        Assert.True(attempt.IsSubmitted);
        _attemptRepoMock.Verify(x => x.SubmitAsync(attempt, It.Is<List<StudentAnswer>>(a => a.Count == 1 && a[0].GradingResult != null && a[0].GradingResult.Score == 1.0m), It.IsAny<CancellationToken>()), Times.Once);
        
        // Verify that the final score was updated
        Assert.Equal(1.0m, attempt.FinalScore);
    }
}
