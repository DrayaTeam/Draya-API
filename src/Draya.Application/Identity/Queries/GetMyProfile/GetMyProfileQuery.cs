using MediatR;

namespace Draya.Application.Identity.Queries.GetMyProfile;

public record GetMyProfileQuery(Guid UserId) : IRequest<object>;
