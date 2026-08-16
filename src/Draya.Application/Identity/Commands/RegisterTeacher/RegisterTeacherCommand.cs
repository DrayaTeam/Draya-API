using Draya.Application.Identity.DTOs;
using MediatR;

namespace Draya.Application.Identity.Commands.RegisterTeacher;

public record RegisterTeacherCommand(
    string Email,
    string Password,
    string ConfirmPassword,
    string FullName,
    string? Phone,
    string? Specialization,
    string? Description
) : IRequest<AuthResponseDto>;
