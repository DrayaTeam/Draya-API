using MediatR;
using Draya.Domain.Classrooms;
using Draya.Application.Classrooms.DTOs;

namespace Draya.Application.Classrooms.Queries.Admin;

public record GetClassroomTypesQuery() : IRequest<List<ClassroomTypeDto>>;
public record GetClassroomTypeByIdQuery(Guid Id) : IRequest<ClassroomTypeDto?>;

public class ClassroomTypeQueryHandlers :
    IRequestHandler<GetClassroomTypesQuery, List<ClassroomTypeDto>>,
    IRequestHandler<GetClassroomTypeByIdQuery, ClassroomTypeDto?>
{
    private readonly IClassroomTypeRepository _repository;

    public ClassroomTypeQueryHandlers(IClassroomTypeRepository repository)
    {
        _repository = repository;
    }

    public async Task<List<ClassroomTypeDto>> Handle(GetClassroomTypesQuery request, CancellationToken cancellationToken)
    {
        var list = await _repository.GetAllAsync(cancellationToken);
        return list.Select(c => new ClassroomTypeDto(c.Id, c.Name, c.Description, c.IsActive, c.CreatedAt)).ToList();
    }

    public async Task<ClassroomTypeDto?> Handle(GetClassroomTypeByIdQuery request, CancellationToken cancellationToken)
    {
        var c = await _repository.GetByIdAsync(request.Id, cancellationToken);
        if (c == null) return null;
        return new ClassroomTypeDto(c.Id, c.Name, c.Description, c.IsActive, c.CreatedAt);
    }
}
