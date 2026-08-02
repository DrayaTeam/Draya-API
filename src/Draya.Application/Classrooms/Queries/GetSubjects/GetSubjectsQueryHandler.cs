using Draya.Application.Classrooms.DTOs;
using Draya.Domain.Classrooms;
using MediatR;

namespace Draya.Application.Classrooms.Queries.GetSubjects;

public class GetSubjectsQueryHandler : IRequestHandler<GetSubjectsQuery, List<SubjectDto>>
{
    private readonly ISubjectRepository _subjectRepository;

    public GetSubjectsQueryHandler(ISubjectRepository subjectRepository)
    {
        _subjectRepository = subjectRepository;
    }

    public async Task<List<SubjectDto>> Handle(GetSubjectsQuery request, CancellationToken cancellationToken)
    {
        var subjects = await _subjectRepository.GetAllAsync(cancellationToken);
        return subjects.Select(s => new SubjectDto(s.Id, s.Name)).ToList();
    }
}
