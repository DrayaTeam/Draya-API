using Draya.Application.Classrooms.DTOs;
using Draya.Domain.Classrooms;
using MediatR;
using System.Linq;

namespace Draya.Application.Classrooms.Queries.GetClassroomTypes;

public class GetClassroomTypesQueryHandler : IRequestHandler<GetClassroomTypesQuery, List<ClassroomTypeDto>>
{
    private readonly IClassroomTypeRepository _classroomTypeRepository;

    public GetClassroomTypesQueryHandler(IClassroomTypeRepository classroomTypeRepository)
    {
        _classroomTypeRepository = classroomTypeRepository;
    }

    public async Task<List<ClassroomTypeDto>> Handle(GetClassroomTypesQuery request, CancellationToken cancellationToken)
    {
        var types = await _classroomTypeRepository.GetAllAsync(cancellationToken);
        return types.Select(t => new ClassroomTypeDto(t.Id, t.Name, t.Description, t.IsActive, t.CreatedAt)).ToList();
    }
}
