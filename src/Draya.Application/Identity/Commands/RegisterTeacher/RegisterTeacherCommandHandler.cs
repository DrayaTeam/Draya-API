using Draya.Application.Common.Interfaces;
using Draya.Application.Identity.DTOs;
using MediatR;

namespace Draya.Application.Identity.Commands.RegisterTeacher;

public class RegisterTeacherCommandHandler : IRequestHandler<RegisterTeacherCommand, AuthResponseDto>
{
    private readonly IIdentityService _identityService;

    public RegisterTeacherCommandHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public Task<AuthResponseDto> Handle(RegisterTeacherCommand request, CancellationToken cancellationToken)
    {
        return _identityService.RegisterTeacherAsync(request.Email, request.Password, request.FullName, request.Phone, cancellationToken);
    }
}
