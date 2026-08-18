using Draya.Application.Classrooms.DTOs;
using Draya.Domain.Classrooms;
using MediatR;

namespace Draya.Application.Classrooms.Queries.GetClassroomSections;

public class GetClassroomSectionsQueryHandler : IRequestHandler<GetClassroomSectionsQuery, List<SectionDto>>
{
    private readonly ISectionRepository _sectionRepository;

    public GetClassroomSectionsQueryHandler(ISectionRepository sectionRepository)
    {
        _sectionRepository = sectionRepository;
    }

    public async Task<List<SectionDto>> Handle(GetClassroomSectionsQuery request, CancellationToken cancellationToken)
    {
        var sections = await _sectionRepository.GetByClassroomIdAsync(request.ClassroomId, cancellationToken);
        
        return sections.Select(s => new SectionDto(
            s.Id,
            s.Title,
            s.Description,
            s.Order,
            s.CreatedAt,
            s.Materials.Select(m => new MaterialDto(
                m.Id,
                m.Title,
                m.MaterialType.ToString(),
                m.CreatedAt,
                m.Versions?.OrderByDescending(v => v.VersionNumber).FirstOrDefault()?.SecureUrl,
                m.VideoDetail?.DurationSeconds
            )).ToList()
        )).ToList();
    }
}
