using Draya.Application.Classrooms.DTOs;
using Draya.Domain.Classrooms;
using MediatR;
using System.Linq;

namespace Draya.Application.Classrooms.Queries.GetGradeLevels;

public class GetGradeLevelsQueryHandler : IRequestHandler<GetGradeLevelsQuery, List<GradeLevelDto>>
{
    private readonly IGradeLevelRepository _gradeLevelRepository;

    public GetGradeLevelsQueryHandler(IGradeLevelRepository gradeLevelRepository)
    {
        _gradeLevelRepository = gradeLevelRepository;
    }

    public async Task<List<GradeLevelDto>> Handle(GetGradeLevelsQuery request, CancellationToken cancellationToken)
    {
        var levels = await _gradeLevelRepository.GetAllAsync(cancellationToken);
        return levels.Select(l => new GradeLevelDto(l.Id, l.Name, l.Description, l.SortOrder, l.IsActive, l.CreatedAt)).ToList();
    }
}
