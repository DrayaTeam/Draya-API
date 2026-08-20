using System;
using System.Threading;
using System.Threading.Tasks;
using Draya.Application.Classrooms.Feedback.Commands;
using Draya.Domain.Classrooms;
using Moq;
using Xunit;

namespace Draya.Application.Tests.Classrooms.Feedback;

public class SubmitClassroomFeedbackCommandHandlerTests
{
    private readonly Mock<IClassroomRepository> _classroomRepoMock = new();
    private readonly Mock<IEnrollmentRepository> _enrollmentRepoMock = new();
    private readonly Mock<IClassroomFeedbackRepository> _feedbackRepoMock = new();
    private readonly SubmitClassroomFeedbackCommandHandler _sut;

    public SubmitClassroomFeedbackCommandHandlerTests()
    {
        _sut = new SubmitClassroomFeedbackCommandHandler(
            _classroomRepoMock.Object,
            _enrollmentRepoMock.Object,
            _feedbackRepoMock.Object);
    }

    [Fact]
    public async Task Handle_FirstTimeFeedback_AddsNewFeedback()
    {
        // Arrange
        var request = new SubmitClassroomFeedbackCommand(Guid.NewGuid(), Guid.NewGuid(), 5, "Great!");
        _classroomRepoMock.Setup(x => x.GetByIdAsync(request.ClassroomId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Classroom { Id = request.ClassroomId });
        _enrollmentRepoMock.Setup(x => x.GetByStudentAndClassroomAsync(request.StudentId, request.ClassroomId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Enrollment { Status = EnrollmentStatus.Active });
            
        // No existing feedback
        _feedbackRepoMock.Setup(x => x.GetFeedbackAsync(request.ClassroomId, request.StudentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ClassroomFeedback?)null);

        // Act
        await _sut.Handle(request, CancellationToken.None);

        // Assert
        _feedbackRepoMock.Verify(x => x.AddAsync(It.Is<ClassroomFeedback>(f => f.Rating == 5 && f.Comment == "Great!"), It.IsAny<CancellationToken>()), Times.Once);
        _feedbackRepoMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ExistingFeedback_UpdatesExistingFeedbackInsteadOfAdding()
    {
        // Arrange
        var request = new SubmitClassroomFeedbackCommand(Guid.NewGuid(), Guid.NewGuid(), 4, "Updated!");
        _classroomRepoMock.Setup(x => x.GetByIdAsync(request.ClassroomId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Classroom { Id = request.ClassroomId });
        _enrollmentRepoMock.Setup(x => x.GetByStudentAndClassroomAsync(request.StudentId, request.ClassroomId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Enrollment { Status = EnrollmentStatus.Active });
            
        // Existing feedback
        var existingFeedback = new ClassroomFeedback { ClassroomId = request.ClassroomId, StudentId = request.StudentId, Rating = 1, Comment = "Bad" };
        _feedbackRepoMock.Setup(x => x.GetFeedbackAsync(request.ClassroomId, request.StudentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingFeedback);

        // Act
        await _sut.Handle(request, CancellationToken.None);

        // Assert
        // We should NOT call AddAsync
        _feedbackRepoMock.Verify(x => x.AddAsync(It.IsAny<ClassroomFeedback>(), It.IsAny<CancellationToken>()), Times.Never);
        // We should call SaveChangesAsync
        _feedbackRepoMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        
        // Ensure values were updated
        Assert.Equal(4, existingFeedback.Rating);
        Assert.Equal("Updated!", existingFeedback.Comment);
    }
}
