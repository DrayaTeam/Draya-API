using Draya.Application.Common.Interfaces;
using Draya.Application.Identity.DTOs;
using MediatR;

namespace Draya.Application.Identity.Commands.RegisterStudent;

public class RegisterStudentCommandHandler : IRequestHandler<RegisterStudentCommand, AuthResponseDto>
{
    private readonly IIdentityService _identityService;

    public RegisterStudentCommandHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public Task<AuthResponseDto> Handle(RegisterStudentCommand request, CancellationToken cancellationToken)
    {
        return _identityService.RegisterStudentAsync(
            request.Email,
            request.Password,
            request.FullName,
            request.ParentGuardianEmail,
            request.ParentGuardianName,
            request.ParentGuardianPhone,
            request.DateOfBirth,
            cancellationToken);
    }
}
