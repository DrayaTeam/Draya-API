using Draya.Application.Classrooms.DTOs;
using MediatR;

namespace Draya.Application.Classrooms.Queries.GetClassroomTypes;

public record GetClassroomTypesQuery() : IRequest<List<ClassroomTypeDto>>;
