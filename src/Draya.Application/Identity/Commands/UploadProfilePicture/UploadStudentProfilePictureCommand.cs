using Draya.Domain.Identity.Exceptions;
using Draya.Application.Materials;
using Draya.Domain.Identity;
using MediatR;

namespace Draya.Application.Identity.Commands.UploadProfilePicture;

public record UploadStudentProfilePictureCommand(
    Guid StudentId,
    Stream Stream,
    string FileName,
    string ContentType
) : IRequest<string>;

public class UploadStudentProfilePictureCommandHandler : IRequestHandler<UploadStudentProfilePictureCommand, string>
{
    private readonly IStudentRepository _studentRepository;
    private readonly IMediaStorageService _mediaStorageService;

    public UploadStudentProfilePictureCommandHandler(
        IStudentRepository studentRepository,
        IMediaStorageService mediaStorageService)
    {
        _studentRepository = studentRepository;
        _mediaStorageService = mediaStorageService;
    }

    public async Task<string> Handle(UploadStudentProfilePictureCommand request, CancellationToken cancellationToken)
    {
        var student = await _studentRepository.GetByUserIdAsync(request.StudentId, cancellationToken);
        if (student == null)
            throw new NotFoundException("Student profile not found.");

        var assetPath = $"Profiles/Students/{request.StudentId}/{Guid.NewGuid()}_{request.FileName}";
        var metadata = await _mediaStorageService.UploadAsync(request.Stream, assetPath, request.ContentType, cancellationToken);
        var imageUrl = await _mediaStorageService.GetSecureDeliveryUrlAsync(metadata);

        student.ProfilePictureUrl = imageUrl;
        await _studentRepository.SaveChangesAsync(cancellationToken);

        return imageUrl;
    }
}
