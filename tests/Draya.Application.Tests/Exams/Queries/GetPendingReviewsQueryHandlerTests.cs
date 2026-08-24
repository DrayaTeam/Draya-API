using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Draya.Application.Exams.Queries.GetPendingReviews;
using Draya.Domain.Classrooms;
using Draya.Domain.Exams;
using Draya.Domain.Identity;
using Moq;
using Xunit;

namespace Draya.Application.Tests.Exams.Queries;

public class GetPendingReviewsQueryHandlerTests
{
    private readonly Mock<IStudentExamAttemptRepository> _mockAttemptRepository;
    private readonly Mock<IExamRepository> _mockExamRepository;
    private readonly Mock<ISectionRepository> _mockSectionRepository;
    private readonly Mock<IClassroomRepository> _mockClassroomRepository;
    private readonly Mock<IStudentRepository> _mockStudentRepository;
    private readonly GetPendingReviewsQueryHandler _handler;

    public GetPendingReviewsQueryHandlerTests()
    {
        _mockAttemptRepository = new Mock<IStudentExamAttemptRepository>();
        _mockExamRepository = new Mock<IExamRepository>();
        _mockSectionRepository = new Mock<ISectionRepository>();
        _mockClassroomRepository = new Mock<IClassroomRepository>();
        _mockStudentRepository = new Mock<IStudentRepository>();
        
        _handler = new GetPendingReviewsQueryHandler(
            _mockAttemptRepository.Object,
            _mockExamRepository.Object,
            _mockSectionRepository.Object,
            _mockClassroomRepository.Object,
            _mockStudentRepository.Object
        );
    }

    [Fact]
    public async Task Handle_ReturnsGroupedPendingReviews_ForSpecificTeacher()
    {
        // Arrange
        var teacherId = Guid.NewGuid();
        var otherTeacherId = Guid.NewGuid();
        
        var classroom1 = new Classroom { Id = Guid.NewGuid(), Name = "Math 101", TeacherId = teacherId };
        var classroom2 = new Classroom { Id = Guid.NewGuid(), Name = "Physics 101", TeacherId = otherTeacherId };
        
        var section1 = new ClassroomSection { Id = Guid.NewGuid(), ClassroomId = classroom1.Id };
        var section2 = new ClassroomSection { Id = Guid.NewGuid(), ClassroomId = classroom2.Id };
        
        var exam1 = new Exam(classroom1.Id, section1.Id, "Midterm Exam", "Algebra", 60, DateTime.UtcNow, null, 1);
        typeof(Exam).GetProperty("Id")?.SetValue(exam1, Guid.NewGuid());
        
        var exam2 = new Exam(classroom2.Id, section2.Id, "Final Exam", "Mechanics", 60, DateTime.UtcNow, null, 1);
        typeof(Exam).GetProperty("Id")?.SetValue(exam2, Guid.NewGuid());

        var student1 = new Student { UserId = Guid.NewGuid(), FullName = "Ahmed" };
        var student2 = new Student { UserId = Guid.NewGuid(), FullName = "Sarah" };

        var attempt1 = new StudentExamAttempt(exam1.Id, student1.UserId);
        attempt1.Submit();
        attempt1.UpdateFinalScore(40, true); // Needs review! Belongs to teacherId

        var attempt2 = new StudentExamAttempt(exam1.Id, student2.UserId);
        attempt2.Submit();
        attempt2.UpdateFinalScore(80, false); // Doesn't need review! Belongs to teacherId

        var attempt3 = new StudentExamAttempt(exam2.Id, student1.UserId);
        attempt3.Submit();
        attempt3.UpdateFinalScore(50, true); // Needs review, but belongs to otherTeacherId!

        _mockClassroomRepository.Setup(r => r.GetQueryable()).Returns(new[] { classroom1, classroom2 }.AsQueryable());
        _mockSectionRepository.Setup(r => r.GetQueryable()).Returns(new[] { section1, section2 }.AsQueryable());
        _mockExamRepository.Setup(r => r.GetQueryable()).Returns(new[] { exam1, exam2 }.AsQueryable());
        _mockStudentRepository.Setup(r => r.GetQueryable()).Returns(new[] { student1, student2 }.AsQueryable());
        _mockAttemptRepository.Setup(r => r.GetQueryable()).Returns(new[] { attempt1, attempt2, attempt3 }.AsQueryable());

        var query = new GetPendingReviewsQuery(teacherId, 1, 10);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Items); // Only classroom1 should be returned
        
        var classroomResult = result.Items.First();
        Assert.Equal(classroom1.Id, classroomResult.ClassroomId);
        Assert.Equal("Math 101", classroomResult.ClassroomName);
        Assert.Single(classroomResult.Exams); // Only exam1
        
        var examResult = classroomResult.Exams.First();
        Assert.Equal(exam1.Id, examResult.ExamId);
        Assert.Single(examResult.PendingReviews); // Only attempt1

        var attemptResult = examResult.PendingReviews.First();
        Assert.Equal(student1.UserId, attemptResult.StudentId);
        Assert.Equal("Ahmed", attemptResult.StudentName);
        Assert.Equal(40, attemptResult.Score);
    }
}
