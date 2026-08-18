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
            s.Materials
                .Where(m => m.MaterialType != Draya.Domain.Materials.MaterialType.Video)
                .Select(m => new DocumentDto(
                    m.Id,
                    m.Title,
                    m.MaterialType.ToString(),
                    m.CreatedAt,
                    m.Versions != null ? m.Versions.OrderByDescending(v => v.VersionNumber).FirstOrDefault()?.SecureUrl : null
                )).ToList(),
            s.Materials
                .Where(m => m.MaterialType == Draya.Domain.Materials.MaterialType.Video)
                .Select(m => new VideoDto(
                    m.Id,
                    m.Title,
                    m.MaterialType.ToString(),
                    m.CreatedAt,
                    m.Versions != null ? m.Versions.OrderByDescending(v => v.VersionNumber).FirstOrDefault()?.SecureUrl : null,
                    m.VideoDetail != null ? m.VideoDetail.DurationSeconds : null
                )).ToList(),
            s.Exams
                .Select(e => new SectionExamDto(
                    e.Id,
                    e.Topic,
                    e.Questions != null ? e.Questions.Count : 0,
                    e.CreatedAt
                )).ToList()
        )).ToList();
    }
}
