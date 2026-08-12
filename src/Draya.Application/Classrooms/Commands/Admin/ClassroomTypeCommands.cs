using MediatR;
using Draya.Domain.Classrooms;
using System.ComponentModel.DataAnnotations;

namespace Draya.Application.Classrooms.Commands.Admin;

public record CreateClassroomTypeCommand(string Name, string Description) : IRequest<Guid>;
public record UpdateClassroomTypeCommand(Guid Id, string Name, string Description, bool IsActive) : IRequest<bool>;
public record DeactivateClassroomTypeCommand(Guid Id) : IRequest<bool>;

public class ClassroomTypeCommandHandlers : 
    IRequestHandler<CreateClassroomTypeCommand, Guid>,
    IRequestHandler<UpdateClassroomTypeCommand, bool>,
    IRequestHandler<DeactivateClassroomTypeCommand, bool>
{
    private readonly IClassroomTypeRepository _repository;

    public ClassroomTypeCommandHandlers(IClassroomTypeRepository repository)
    {
        _repository = repository;
    }

    public async Task<Guid> Handle(CreateClassroomTypeCommand request, CancellationToken cancellationToken)
    {
        var entity = new ClassroomType
        {
            Name = request.Name,
            Description = request.Description,
            IsActive = true
        };
        await _repository.AddAsync(entity, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);
        return entity.Id;
    }

    public async Task<bool> Handle(UpdateClassroomTypeCommand request, CancellationToken cancellationToken)
    {
        var entity = await _repository.GetByIdAsync(request.Id, cancellationToken);
        if (entity == null) return false;

        entity.Name = request.Name;
        entity.Description = request.Description;
        entity.IsActive = request.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;

        await _repository.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> Handle(DeactivateClassroomTypeCommand request, CancellationToken cancellationToken)
    {
        var entity = await _repository.GetByIdAsync(request.Id, cancellationToken);
        if (entity == null) return false;

        entity.IsActive = false;
        entity.UpdatedAt = DateTime.UtcNow;

        await _repository.SaveChangesAsync(cancellationToken);
        return true;
    }
}
