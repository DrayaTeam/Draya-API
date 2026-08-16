using Draya.Domain.Identity.Exceptions;
using Draya.Application.Materials;
using Draya.Domain.Identity;
using MediatR;

namespace Draya.Application.Identity.Commands.UploadProfilePicture;

public record UploadTeacherProfilePictureCommand(
    Guid TeacherId,
    Stream Stream,
    string FileName,
    string ContentType
) : IRequest<string>;

public class UploadTeacherProfilePictureCommandHandler : IRequestHandler<UploadTeacherProfilePictureCommand, string>
{
    private readonly ITeacherRepository _teacherRepository;
    private readonly IMediaStorageService _mediaStorageService;

    public UploadTeacherProfilePictureCommandHandler(
        ITeacherRepository teacherRepository,
        IMediaStorageService mediaStorageService)
    {
        _teacherRepository = teacherRepository;
        _mediaStorageService = mediaStorageService;
    }

    public async Task<string> Handle(UploadTeacherProfilePictureCommand request, CancellationToken cancellationToken)
    {
        var teacher = await _teacherRepository.GetByUserIdAsync(request.TeacherId, cancellationToken);
        if (teacher == null)
            throw new NotFoundException("Teacher profile not found.");

        var assetPath = $"Profiles/Teachers/{request.TeacherId}/{Guid.NewGuid()}_{request.FileName}";
        var metadata = await _mediaStorageService.UploadAsync(request.Stream, assetPath, request.ContentType, cancellationToken);
        var imageUrl = await _mediaStorageService.GetSecureDeliveryUrlAsync(metadata);

        teacher.ProfilePictureUrl = imageUrl;
        await _teacherRepository.SaveChangesAsync(cancellationToken);

        return imageUrl;
    }
}
