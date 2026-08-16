using Draya.Application.Identity.DTOs;
using MediatR;

namespace Draya.Application.Identity.Queries.GetTeacherById;

public record GetTeacherByIdQuery(Guid TeacherId) : IRequest<TeacherProfileDto>;
