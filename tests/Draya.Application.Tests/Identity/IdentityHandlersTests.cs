using Draya.Application.Common.Interfaces;
using Draya.Application.Identity.Commands.ConfirmPasswordReset;
using Draya.Application.Identity.Commands.Login;
using Draya.Application.Identity.Commands.Logout;
using Draya.Application.Identity.Commands.RefreshToken;
using Draya.Application.Identity.Commands.RegisterStudent;
using Draya.Application.Identity.Commands.RegisterTeacher;
using Draya.Application.Identity.Commands.RequestPasswordReset;
using Draya.Application.Identity.DTOs;
using Draya.Application.Identity.Queries.GetMyProfile;
using Moq;

namespace Draya.Application.Tests.Identity;

public class IdentityHandlersTests
{
    private readonly Mock<IIdentityService> _identityServiceMock;

    public IdentityHandlersTests()
    {
        _identityServiceMock = new Mock<IIdentityService>();
    }

    [Fact]
    public async Task LoginCommandHandler_DelegatesToIdentityService()
    {
        var expectedResponse = new AuthResponseDto("access_token", "refresh_token", 3600, new UserSummaryDto(Guid.NewGuid(), "Teacher Name", "Teacher"));
        _identityServiceMock.Setup(s => s.LoginAsync("teacher@test.com", "Password123!", It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        var handler = new LoginCommandHandler(_identityServiceMock.Object);
        var result = await handler.Handle(new LoginCommand("teacher@test.com", "Password123!"), CancellationToken.None);

        Assert.Equal(expectedResponse, result);
        _identityServiceMock.Verify(s => s.LoginAsync("teacher@test.com", "Password123!", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RegisterTeacherCommandHandler_DelegatesToIdentityService()
    {
        var expectedResponse = new AuthResponseDto("access_token", "refresh_token", 3600, new UserSummaryDto(Guid.NewGuid(), "Teacher Name", "Teacher"));
        _identityServiceMock.Setup(s => s.RegisterTeacherAsync("teacher@test.com", "Password123!", "Teacher Name", "123456", null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        var handler = new RegisterTeacherCommandHandler(_identityServiceMock.Object);
        var result = await handler.Handle(new RegisterTeacherCommand("teacher@test.com", "Password123!", "Password123!", "Teacher Name", "123456", null, null), CancellationToken.None);

        Assert.Equal(expectedResponse, result);
        _identityServiceMock.Verify(s => s.RegisterTeacherAsync("teacher@test.com", "Password123!", "Teacher Name", "123456", null, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RegisterStudentCommandHandler_DelegatesToIdentityService()
    {
        var expectedResponse = new AuthResponseDto("access_token", "refresh_token", 3600, new UserSummaryDto(Guid.NewGuid(), "Student Name", "Student"));
        _identityServiceMock.Setup(s => s.RegisterStudentAsync("student@test.com", "Password123!", "Student Name", "parent@test.com", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        var handler = new RegisterStudentCommandHandler(_identityServiceMock.Object);
        var result = await handler.Handle(new RegisterStudentCommand("student@test.com", "Password123!", "Password123!", "Student Name", "parent@test.com", null), CancellationToken.None);

        Assert.Equal(expectedResponse, result);
        _identityServiceMock.Verify(s => s.RegisterStudentAsync("student@test.com", "Password123!", "Student Name", "parent@test.com", null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RefreshTokenCommandHandler_DelegatesToIdentityService()
    {
        var expectedResponse = new AuthResponseDto("new_access_token", "new_refresh_token", 3600, new UserSummaryDto(Guid.NewGuid(), "Teacher Name", "Teacher"));
        _identityServiceMock.Setup(s => s.RefreshTokenAsync("old_refresh_token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        var handler = new RefreshTokenCommandHandler(_identityServiceMock.Object);
        var result = await handler.Handle(new RefreshTokenCommand("old_refresh_token"), CancellationToken.None);

        Assert.Equal(expectedResponse, result);
        _identityServiceMock.Verify(s => s.RefreshTokenAsync("old_refresh_token", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LogoutCommandHandler_DelegatesToIdentityService()
    {
        var userId = Guid.NewGuid();
        var handler = new LogoutCommandHandler(_identityServiceMock.Object);

        await handler.Handle(new LogoutCommand(userId, "token_val"), CancellationToken.None);

        _identityServiceMock.Verify(s => s.LogoutAsync(userId, "token_val", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RequestPasswordResetCommandHandler_DelegatesToIdentityService()
    {
        var expectedResponse = new PasswordResetRequestResponseDto("If the account exists, a password reset email has been sent.");
        _identityServiceMock.Setup(s => s.RequestPasswordResetAsync("user@test.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        var handler = new RequestPasswordResetCommandHandler(_identityServiceMock.Object);
        var result = await handler.Handle(new RequestPasswordResetCommand("user@test.com"), CancellationToken.None);

        Assert.Equal(expectedResponse, result);
        _identityServiceMock.Verify(s => s.RequestPasswordResetAsync("user@test.com", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ConfirmPasswordResetCommandHandler_DelegatesToIdentityService()
    {
        var handler = new ConfirmPasswordResetCommandHandler(_identityServiceMock.Object);

        await handler.Handle(new ConfirmPasswordResetCommand("reset_token", "NewPassword123!"), CancellationToken.None);

        _identityServiceMock.Verify(s => s.ConfirmPasswordResetAsync("reset_token", "NewPassword123!", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetMyProfileQueryHandler_DelegatesToIdentityService()
    {
        var userId = Guid.NewGuid();
        var expectedProfile = new UserSummaryDto(userId, "Teacher Name", "Teacher");
        _identityServiceMock.Setup(s => s.GetUserProfileAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedProfile);

        var handler = new GetMyProfileQueryHandler(_identityServiceMock.Object);
        var result = await handler.Handle(new GetMyProfileQuery(userId), CancellationToken.None);

        Assert.Equal(expectedProfile, result);
        _identityServiceMock.Verify(s => s.GetUserProfileAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
    }
}
