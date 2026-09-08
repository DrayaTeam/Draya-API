using Draya.Application.Identity.DTOs;
using MediatR;

namespace Draya.Application.Identity.Queries.GetSupervisors;

public record GetSupervisorsQuery : IRequest<List<SupervisorDto>>;
