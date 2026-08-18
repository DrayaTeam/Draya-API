using MediatR;

namespace Draya.Application.Classrooms.Commands.UploadClassroomImage;

public record UploadClassroomImageCommand(
    Guid ClassroomId,
    Guid TeacherId,
    Stream ImageStream,
    string FileName,
    string ContentType
) : IRequest<string>;
