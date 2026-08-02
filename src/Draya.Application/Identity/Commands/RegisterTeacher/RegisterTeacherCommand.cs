using Draya.Application.Identity.DTOs;
using MediatR;

namespace Draya.Application.Identity.Commands.RegisterTeacher;

public record RegisterTeacherCommand(
    string Email,
    string Password,
    string FullName,
    string? Phone
) : IRequest<AuthResponseDto>;
