using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Draya.Application.Classrooms.Queries.GetClassroomDetails;
using Draya.Domain.Classrooms;
using Draya.Domain.Classrooms.Exceptions;
using Draya.Domain.Materials;
using Moq;
using Xunit;

namespace Draya.Application.Tests.Classrooms.Queries.GetClassroomDetails;

public class GetClassroomDetailsQueryHandlerTests
{
    private readonly Mock<IClassroomRepository> _classroomRepositoryMock;
    private readonly Mock<IEnrollmentRepository> _enrollmentRepositoryMock;
    private readonly Mock<IMaterialRepository> _materialRepositoryMock;
    private readonly Mock<Draya.Domain.Identity.ITeacherRepository> _teacherRepositoryMock;
    private readonly Mock<ISectionRepository> _sectionRepositoryMock;
    private readonly GetClassroomDetailsQueryHandler _handler;

    public GetClassroomDetailsQueryHandlerTests()
    {
        _classroomRepositoryMock = new Mock<IClassroomRepository>();
        _enrollmentRepositoryMock = new Mock<IEnrollmentRepository>();
        _materialRepositoryMock = new Mock<IMaterialRepository>();
        _teacherRepositoryMock = new Mock<Draya.Domain.Identity.ITeacherRepository>();
        _sectionRepositoryMock = new Mock<ISectionRepository>();

        _handler = new GetClassroomDetailsQueryHandler(
            _classroomRepositoryMock.Object,
            _enrollmentRepositoryMock.Object,
            _materialRepositoryMock.Object,
            _teacherRepositoryMock.Object,
            _sectionRepositoryMock.Object
        );
    }

    [Fact]
    public async Task Handle_PopulatesSectionsAndLessonsCount()
    {
        // Arrange
        var classroomId = Guid.NewGuid();
        var teacherId = Guid.NewGuid();
        var query = new GetClassroomDetailsQuery(classroomId, Guid.NewGuid(), "Student");
        var classroom = new Classroom
        {
            Id = classroomId,
            TeacherId = teacherId,
            Name = "Test Class",
            Subject = new Subject { Name = "Math" },
            ClassroomType = new ClassroomType { Name = "Online" },
            GradeLevel = new GradeLevel { Name = "10th Grade" }
        };
        var sections = new List<ClassroomSection>
        {
            new ClassroomSection { ClassroomId = classroomId, Title = "Section 1", Order = 1 },
            new ClassroomSection { ClassroomId = classroomId, Title = "Section 2", Order = 2 }
        };
        
        _classroomRepositoryMock.Setup(r => r.GetByIdAsync(classroomId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(classroom);
        _teacherRepositoryMock.Setup(r => r.GetByUserIdAsync(teacherId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Draya.Domain.Identity.Teacher { UserId = teacherId });
            
        // Mock sections
        _sectionRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(sections.AsQueryable());
            
        // Mock materials (lessons)
        _materialRepositoryMock.Setup(r => r.GetCountByClassroomIdAsync(classroomId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(5);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.SectionsCount);
        Assert.Equal(5, result.LessonsCount);
    }
}
