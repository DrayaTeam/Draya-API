using Draya.Application.Classrooms.DTOs;
using MediatR;

namespace Draya.Application.Classrooms.Commands.CreateSubject;

public record CreateSubjectCommand(string Name) : IRequest<SubjectDto>;
