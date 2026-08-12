using MediatR;
using Draya.Domain.Classrooms;

namespace Draya.Application.Classrooms.Commands.Admin;

public record CreateGradeLevelCommand(string Name, string Description, int SortOrder) : IRequest<Guid>;
public record UpdateGradeLevelCommand(Guid Id, string Name, string Description, int SortOrder, bool IsActive) : IRequest<bool>;
public record DeactivateGradeLevelCommand(Guid Id) : IRequest<bool>;

public class GradeLevelCommandHandlers : 
    IRequestHandler<CreateGradeLevelCommand, Guid>,
    IRequestHandler<UpdateGradeLevelCommand, bool>,
    IRequestHandler<DeactivateGradeLevelCommand, bool>
{
    private readonly IGradeLevelRepository _repository;

    public GradeLevelCommandHandlers(IGradeLevelRepository repository)
    {
        _repository = repository;
    }

    public async Task<Guid> Handle(CreateGradeLevelCommand request, CancellationToken cancellationToken)
    {
        var entity = new GradeLevel
        {
            Name = request.Name,
            Description = request.Description,
            SortOrder = request.SortOrder,
            IsActive = true
        };
        await _repository.AddAsync(entity, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);
        return entity.Id;
    }

    public async Task<bool> Handle(UpdateGradeLevelCommand request, CancellationToken cancellationToken)
    {
        var entity = await _repository.GetByIdAsync(request.Id, cancellationToken);
        if (entity == null) return false;

        entity.Name = request.Name;
        entity.Description = request.Description;
        entity.SortOrder = request.SortOrder;
        entity.IsActive = request.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;

        await _repository.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> Handle(DeactivateGradeLevelCommand request, CancellationToken cancellationToken)
    {
        var entity = await _repository.GetByIdAsync(request.Id, cancellationToken);
        if (entity == null) return false;

        entity.IsActive = false;
        entity.UpdatedAt = DateTime.UtcNow;

        await _repository.SaveChangesAsync(cancellationToken);
        return true;
    }
}
