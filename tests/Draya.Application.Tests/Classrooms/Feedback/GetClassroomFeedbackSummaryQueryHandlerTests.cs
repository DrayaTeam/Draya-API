using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Draya.Application.Classrooms.Feedback.Queries;
using Draya.Domain.Classrooms;
using Draya.Domain.Identity;
using Moq;
using Xunit;

namespace Draya.Application.Tests.Classrooms.Feedback;

public class GetClassroomFeedbackSummaryQueryHandlerTests
{
    private readonly Mock<IClassroomRepository> _classroomRepoMock = new();
    private readonly Mock<IClassroomFeedbackRepository> _feedbackRepoMock = new();
    private readonly Mock<IStudentRepository> _studentRepoMock = new();
    private readonly GetClassroomFeedbackSummaryQueryHandler _sut;

    public GetClassroomFeedbackSummaryQueryHandlerTests()
    {
        _sut = new GetClassroomFeedbackSummaryQueryHandler(
            _classroomRepoMock.Object,
            _feedbackRepoMock.Object,
            _studentRepoMock.Object);
    }

    [Fact]
    public async Task Handle_ZeroFeedbacks_ReturnsEarlyWithoutQueryingStudents()
    {
        // Arrange
        var request = new GetClassroomFeedbackSummaryQuery(Guid.NewGuid(), 1, 10);
        
        _classroomRepoMock.Setup(x => x.GetByIdAsync(request.ClassroomId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Classroom { Id = request.ClassroomId });

        _feedbackRepoMock.Setup(x => x.GetByClassroomIdAsync(request.ClassroomId, 1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<ClassroomFeedback>(), 0, 0.0));

        // Act
        var result = await _sut.Handle(request, CancellationToken.None);

        // Assert
        Assert.Equal(0, result.TotalCount);
        Assert.Equal(0.0, result.AverageRating);
        Assert.Empty(result.Items);
        
        // Ensure student repository was NEVER called because of early exit
        _studentRepoMock.Verify(x => x.GetByUserIdsAsync(It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
