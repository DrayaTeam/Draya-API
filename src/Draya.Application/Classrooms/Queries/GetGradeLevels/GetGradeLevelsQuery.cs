using Draya.Application.Classrooms.DTOs;
using MediatR;

namespace Draya.Application.Classrooms.Queries.GetGradeLevels;

public record GetGradeLevelsQuery() : IRequest<List<GradeLevelDto>>;
