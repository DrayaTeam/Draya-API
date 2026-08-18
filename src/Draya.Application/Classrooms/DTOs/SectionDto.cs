namespace Draya.Application.Classrooms.DTOs;

public record SectionDto(
    Guid Id,
    string Title,
    string Description,
    int Order,
    DateTime CreatedAt,
    List<DocumentDto> Documents,
    List<VideoDto> Videos,
    List<SectionExamDto> Exams
);

public record SectionExamDto(
    Guid Id,
    string Topic,
    int QuestionsCount,
    DateTime CreatedAt
);

public record DocumentDto(
    Guid Id,
    string Title,
    string MaterialType,
    DateTime CreatedAt,
    string? FileUrl
);

public record VideoDto(
    Guid Id,
    string Title,
    string MaterialType,
    DateTime CreatedAt,
    string? VideoUrl,
    int? VideoDurationInSeconds
);
