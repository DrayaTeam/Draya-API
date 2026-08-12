using MediatR;

namespace Draya.Application.Classrooms.Commands.CheckoutClassroom;

public record CheckoutClassroomCommand(Guid StudentId, Guid ClassroomId) : IRequest<string>;
