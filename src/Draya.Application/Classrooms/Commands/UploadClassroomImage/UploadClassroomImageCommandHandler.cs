using Draya.Application.Materials;
using Draya.Domain.Classrooms;
using Draya.Domain.Classrooms.Exceptions;
using MediatR;

namespace Draya.Application.Classrooms.Commands.UploadClassroomImage;

public class UploadClassroomImageCommandHandler : IRequestHandler<UploadClassroomImageCommand, string>
{
    private readonly IClassroomRepository _classroomRepository;
    private readonly IMediaStorageService _mediaStorageService;

    public UploadClassroomImageCommandHandler(
        IClassroomRepository classroomRepository,
        IMediaStorageService mediaStorageService)
    {
        _classroomRepository = classroomRepository;
        _mediaStorageService = mediaStorageService;
    }

    public async Task<string> Handle(UploadClassroomImageCommand request, CancellationToken cancellationToken)
    {
        var classroom = await _classroomRepository.GetByIdAsync(request.ClassroomId, cancellationToken);
        if (classroom is null || classroom.TeacherId != request.TeacherId)
        {
            throw new ClassroomNotFoundException();
        }

        var extension = Path.GetExtension(request.FileName);
        var storageFileName = $"classrooms/{classroom.Id}/cover{extension}";
        var metadata = await _mediaStorageService.UploadAsync(
            request.ImageStream,
            storageFileName,
            request.ContentType,
            cancellationToken);

        var imageUrl = await _mediaStorageService.GetSecureDeliveryUrlAsync(metadata);
        classroom.ImageUrl = imageUrl;
        await _classroomRepository.SaveChangesAsync(cancellationToken);

        return imageUrl;
    }
}
