using Draya.Application.Identity.DTOs;
using MediatR;

namespace Draya.Application.Identity.Commands.RegisterStudent;

public record RegisterStudentCommand(
    string Email,
    string Password,
    string ConfirmPassword,
    string FullName,
    string ParentGuardianEmail,
    string ParentGuardianName,
    string ParentGuardianPhone,
    DateTime? DateOfBirth
) : IRequest<AuthResponseDto>;
