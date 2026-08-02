using Draya.Application.Classrooms.DTOs;
using MediatR;

namespace Draya.Application.Classrooms.Queries.GetSubjects;

public record GetSubjectsQuery : IRequest<List<SubjectDto>>;
