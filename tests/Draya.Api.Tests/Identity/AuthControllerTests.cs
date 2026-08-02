using Draya.Api.Controllers.Identity;
using Draya.Application.Identity.Commands.Logout;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Draya.Api.Tests.Identity;

public class AuthControllerTests
{
    [Fact]
    public async Task Logout_WithoutBody_ReturnsNoContent_AndSendsLogoutCommand()
    {
        var mediator = new FakeMediator();
        var controller = new AuthController(mediator)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())
                    }, "Test"))
                }
            }
        };

        var result = await controller.Logout(CancellationToken.None);

        var noContent = Assert.IsType<NoContentResult>(result);
        Assert.Equal(StatusCodes.Status204NoContent, noContent.StatusCode);
        Assert.IsType<LogoutCommand>(mediator.LastRequest);
    }

    private sealed class FakeMediator : IMediator
    {
        public object? LastRequest { get; private set; }

        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            LastRequest = request;
            return Task.FromResult((TResponse)(object)Unit.Value!);
        }

        public Task Publish(object notification, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default) where TNotification : INotification => Task.CompletedTask;

        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        Task ISender.Send<TRequest>(TRequest request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            return Task.CompletedTask;
        }

        Task<object?> ISender.Send(object request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            return Task.FromResult<object?>(Unit.Value);
        }

        Task<TResponse> ISender.Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            return Task.FromResult((TResponse)(object)Unit.Value!);
        }
    }
}
