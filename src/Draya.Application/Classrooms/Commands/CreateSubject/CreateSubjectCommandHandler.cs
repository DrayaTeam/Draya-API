using Draya.Application.Classrooms.DTOs;
using Draya.Domain.Classrooms;
using MediatR;

namespace Draya.Application.Classrooms.Commands.CreateSubject;

public class CreateSubjectCommandHandler : IRequestHandler<CreateSubjectCommand, SubjectDto>
{
    private readonly ISubjectRepository _subjectRepository;

    public CreateSubjectCommandHandler(ISubjectRepository subjectRepository)
    {
        _subjectRepository = subjectRepository;
    }

    public async Task<SubjectDto> Handle(CreateSubjectCommand request, CancellationToken cancellationToken)
    {
        var normalizedName = request.Name.Trim();
        
        var existing = await _subjectRepository.GetByNameAsync(normalizedName, cancellationToken);
        if (existing != null)
        {
            return new SubjectDto(existing.Id, existing.Name);
        }

        var subject = new Subject
        {
            Name = normalizedName
        };

        await _subjectRepository.AddAsync(subject, cancellationToken);
        await _subjectRepository.SaveChangesAsync(cancellationToken);

        return new SubjectDto(subject.Id, subject.Name);
    }
}
