using Draya.Application.Identity.DTOs;
using MediatR;

namespace Draya.Application.Identity.Queries.SearchTeachersForAdmin;

public record SearchTeachersForAdminQuery(string? Query) : IRequest<List<TeacherSearchDto>>;
