namespace Draya.Application.Classrooms.DTOs;

public record SectionDto(
    Guid Id,
    string Title,
    string Description,
    int Order,
    DateTime CreatedAt,
    List<MaterialDto> Materials
);

public record MaterialDto(
    Guid Id,
    string Title,
    string MaterialType,
    DateTime CreatedAt,
    string? VideoUrl,
    int? VideoDurationInSeconds
);
