using MediatR;
using Draya.Domain.Classrooms;
using Draya.Application.Classrooms.DTOs;

namespace Draya.Application.Classrooms.Queries.Admin;

public record GetGradeLevelsQuery() : IRequest<List<GradeLevelDto>>;
public record GetGradeLevelByIdQuery(Guid Id) : IRequest<GradeLevelDto?>;

public class GradeLevelQueryHandlers :
    IRequestHandler<GetGradeLevelsQuery, List<GradeLevelDto>>,
    IRequestHandler<GetGradeLevelByIdQuery, GradeLevelDto?>
{
    private readonly IGradeLevelRepository _repository;

    public GradeLevelQueryHandlers(IGradeLevelRepository repository)
    {
        _repository = repository;
    }

    public async Task<List<GradeLevelDto>> Handle(GetGradeLevelsQuery request, CancellationToken cancellationToken)
    {
        var list = await _repository.GetAllAsync(cancellationToken);
        return list.Select(g => new GradeLevelDto(g.Id, g.Name, g.Description, g.SortOrder, g.IsActive, g.CreatedAt)).ToList();
    }

    public async Task<GradeLevelDto?> Handle(GetGradeLevelByIdQuery request, CancellationToken cancellationToken)
    {
        var g = await _repository.GetByIdAsync(request.Id, cancellationToken);
        if (g == null) return null;
        return new GradeLevelDto(g.Id, g.Name, g.Description, g.SortOrder, g.IsActive, g.CreatedAt);
    }
}
