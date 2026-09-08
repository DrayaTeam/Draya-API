using Draya.Application.Identity.DTOs;
using MediatR;

namespace Draya.Application.Identity.Queries.GetTeachers;

public record GetTeachersQuery(string? Specialization) : IRequest<List<TeacherProfileDto>>;
