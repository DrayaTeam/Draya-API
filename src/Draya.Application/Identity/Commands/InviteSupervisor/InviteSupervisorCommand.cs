using Draya.Application.Identity.DTOs;
using MediatR;

namespace Draya.Application.Identity.Commands.InviteSupervisor;

public record InviteSupervisorCommand(string Name, string Email, string Role) : IRequest<SupervisorDto>;
