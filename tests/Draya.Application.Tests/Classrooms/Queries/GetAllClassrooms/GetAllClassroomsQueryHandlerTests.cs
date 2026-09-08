using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Draya.Application.Classrooms.Queries.GetAllClassrooms;
using Draya.Domain.Classrooms;
using Draya.Domain.Materials;
using Moq;
using Xunit;

namespace Draya.Application.Tests.Classrooms.Queries.GetAllClassrooms;

public class GetAllClassroomsQueryHandlerTests
{
    private readonly Mock<IClassroomRepository> _classroomRepositoryMock;
    private readonly Mock<IMaterialRepository> _materialRepositoryMock;
    private readonly Mock<Draya.Domain.Identity.ITeacherRepository> _teacherRepositoryMock;
    private readonly Mock<ISectionRepository> _sectionRepositoryMock;
    private readonly GetAllClassroomsQueryHandler _handler;

    public GetAllClassroomsQueryHandlerTests()
    {
        _classroomRepositoryMock = new Mock<IClassroomRepository>();
        _materialRepositoryMock = new Mock<IMaterialRepository>();
        _teacherRepositoryMock = new Mock<Draya.Domain.Identity.ITeacherRepository>();
        _sectionRepositoryMock = new Mock<ISectionRepository>();

        _handler = new GetAllClassroomsQueryHandler(
            _classroomRepositoryMock.Object,
            _materialRepositoryMock.Object,
            _teacherRepositoryMock.Object,
            _sectionRepositoryMock.Object
        );
    }

    [Fact]
    public async Task Handle_PopulatesSectionsAndLessonsCount_ForAllItems()
    {
        // Arrange
        var query = new GetAllClassroomsQuery(1, 10);
        var classroomId1 = Guid.NewGuid();
        var classroomId2 = Guid.NewGuid();
        var teacherId = Guid.NewGuid();
        
        var classrooms = new List<Classroom>
        {
            new Classroom { Id = classroomId1, TeacherId = teacherId, Name = "Class 1" },
            new Classroom { Id = classroomId2, TeacherId = teacherId, Name = "Class 2" }
        };

        _classroomRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(classrooms.AsQueryable());

        _teacherRepositoryMock.Setup(r => r.GetByUserIdAsync(teacherId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Draya.Domain.Identity.Teacher { UserId = teacherId });

        var sections = new List<ClassroomSection>
        {
            new ClassroomSection { ClassroomId = classroomId1, Title = "Section 1", Order = 1 },
            new ClassroomSection { ClassroomId = classroomId2, Title = "Section 2", Order = 1 },
            new ClassroomSection { ClassroomId = classroomId2, Title = "Section 3", Order = 2 }
        };

        _sectionRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(sections.AsQueryable());

        _materialRepositoryMock.Setup(r => r.GetCountByClassroomIdAsync(classroomId1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(3);
            
        _materialRepositoryMock.Setup(r => r.GetCountByClassroomIdAsync(classroomId2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(5);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.TotalCount);
        
        var dto1 = result.Items.First(c => c.ClassroomId == classroomId1);
        Assert.Equal(1, dto1.SectionsCount);
        Assert.Equal(3, dto1.LessonsCount);
        
        var dto2 = result.Items.First(c => c.ClassroomId == classroomId2);
        Assert.Equal(2, dto2.SectionsCount);
        Assert.Equal(5, dto2.LessonsCount);
    }
}
