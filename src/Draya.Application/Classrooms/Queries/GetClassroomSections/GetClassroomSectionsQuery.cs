using Draya.Application.Classrooms.DTOs;
using MediatR;

namespace Draya.Application.Classrooms.Queries.GetClassroomSections;

public record GetClassroomSectionsQuery(Guid ClassroomId) : IRequest<List<SectionDto>>;
