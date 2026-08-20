using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Draya.Application.AI;
using Draya.Application.AI.Models;
using Draya.Application.Common.Interfaces;
using Draya.Application.Exams.Services;
using Draya.Application.Materials;
using Draya.Application.Materials.RAG;
using Draya.Domain.Classrooms;
using Draya.Domain.Exams;
using Draya.Domain.Materials;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Draya.Application.Tests.Exams.Services;

public class ExamGenerationServiceTests
{
    private readonly Mock<IExamGenerationRepository> _generationRepoMock = new();
    private readonly Mock<IExamRepository> _examRepoMock = new();
    private readonly Mock<IExamGenerationTaskQueue> _taskQueueMock = new();
    private readonly Mock<IRetrievalService> _retrievalServiceMock = new();
    private readonly Mock<ILLMService> _llmServiceMock = new();
    private readonly Mock<IPiiAnonymizer> _piiAnonymizerMock = new();
    private readonly Mock<IMaterialRepository> _materialRepoMock = new();
    private readonly Mock<ILogger<ExamGenerationService>> _loggerMock = new();
    private readonly Mock<IPublisher> _publisherMock = new();

    private readonly ExamGenerationService _sut;

    public ExamGenerationServiceTests()
    {
        _sut = new ExamGenerationService(
            _generationRepoMock.Object,
            _examRepoMock.Object,
            _taskQueueMock.Object,
            _retrievalServiceMock.Object,
            _llmServiceMock.Object,
            _piiAnonymizerMock.Object,
            _materialRepoMock.Object,
            _loggerMock.Object,
            _publisherMock.Object);
    }

    [Fact]
    public async Task ProcessGenerationAsync_NoRagContext_FailsGracefully()
    {
        // Arrange
        var request = new GenerateExamRequest { SectionId = Guid.NewGuid(), Topic = "Test" };
        var generationId = Guid.NewGuid();
        var generation = new ExamGeneration(Guid.NewGuid(), Guid.NewGuid(), request.SectionId, 10, "key");
        
        // Mock generation repo
        _generationRepoMock.Setup(x => x.GetByIdAsync(generationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(generation);

        // Mock no materials found
        _materialRepoMock.Setup(x => x.GetParsedMaterialVersionIdsBySectionIdAsync(request.SectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Guid>());

        // Act
        await _sut.ProcessGenerationAsync(generationId, request);

        // Assert
        Assert.Equal(GenerationStatus.Failed, generation.Status);
        Assert.Equal("No parsed materials found in this section to generate an exam from.", generation.ErrorMessage);
        _llmServiceMock.Verify(x => x.GenerateAsync(It.IsAny<LlmRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ProcessGenerationAsync_BatchSlicingLogic_WorksCorrectly()
    {
        // Arrange: Request 105 questions (should be 7 batches of 15)
        var request = new GenerateExamRequest 
        { 
            SectionId = Guid.NewGuid(), 
            Topic = "Test",
            DurationMinutes = 60,
            StartDate = DateTime.UtcNow,
            QuestionRequirements = new List<QuestionTypeRequirement>
            {
                new QuestionTypeRequirement { Type = "MultipleChoice", Count = 105 }
            }
        };
        var generationId = Guid.NewGuid();
        var generation = new ExamGeneration(Guid.NewGuid(), Guid.NewGuid(), request.SectionId, 105, "key");
        
        _generationRepoMock.Setup(x => x.GetByIdAsync(generationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(generation);

        // 45 chunks
        _materialRepoMock.Setup(x => x.GetParsedMaterialVersionIdsBySectionIdAsync(request.SectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Guid> { Guid.NewGuid() });
            
        var chunks = Enumerable.Range(0, 45).Select(i => new RetrievedChunk { ChunkId = Guid.NewGuid().ToString(), Text = $"Chunk {i}" }).ToList();
        _retrievalServiceMock.Setup(x => x.SearchAsync(It.IsAny<RetrievalQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(chunks);

        // Mock LLM returning valid JSON format with questions for each batch
        _llmServiceMock.Setup(x => x.GenerateAsync(It.IsAny<LlmRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((LlmRequest req, CancellationToken _) => 
            {
                var q = new GeneratedQuestionDto 
                { 
                    Text = "Mock Question", 
                    Type = "MultipleChoice", 
                    Difficulty = "Medium", 
                    SourceChunkIds = new List<string> { chunks[0].ChunkId }, // Fake a valid chunk reference
                    Options = new List<GeneratedQuestionOptionDto> { new() { Text = "A" }, new() { Text = "B" } },
                    CorrectAnswerIndex = 0
                };
                
                var mockQuestions = new List<GeneratedQuestionDto>();
                // The batch loop requests 15 questions per batch
                for (int i=0; i<15; i++) mockQuestions.Add(q);

                var response = new GeneratedExamDto { Status = "success", Questions = mockQuestions };
                return new LlmResponse { Content = JsonSerializer.Serialize(response, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }) };
            });

        // Act
        await _sut.ProcessGenerationAsync(generationId, request);

        // Assert
        // 105 questions requested -> 7 batches of 15
        _llmServiceMock.Verify(x => x.GenerateAsync(It.IsAny<LlmRequest>(), It.IsAny<CancellationToken>()), Times.Exactly(7));
        
        // Assert Exam entity created and saved with all 105 valid questions
        _examRepoMock.Verify(x => x.AddAsync(It.Is<Exam>(e => e.Questions.Count == 105), It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal(GenerationStatus.Completed, generation.Status);
    }
    
    [Fact]
    public async Task ProcessGenerationAsync_LlmHallucination_HandlesGracefully()
    {
        // Arrange: Request 30 questions (2 batches)
        var request = new GenerateExamRequest 
        { 
            SectionId = Guid.NewGuid(), 
            Topic = "Test",
            DurationMinutes = 60,
            StartDate = DateTime.UtcNow,
            QuestionRequirements = new List<QuestionTypeRequirement>
            {
                new QuestionTypeRequirement { Type = "MultipleChoice", Count = 30 }
            }
        };
        var generationId = Guid.NewGuid();
        var generation = new ExamGeneration(Guid.NewGuid(), Guid.NewGuid(), request.SectionId, 30, "key");
        
        _generationRepoMock.Setup(x => x.GetByIdAsync(generationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(generation);

        // Chunks
        _materialRepoMock.Setup(x => x.GetParsedMaterialVersionIdsBySectionIdAsync(request.SectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Guid> { Guid.NewGuid() });
            
        var chunkId = Guid.NewGuid().ToString();
        _retrievalServiceMock.Setup(x => x.SearchAsync(It.IsAny<RetrievalQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RetrievedChunk> { new() { ChunkId = chunkId, Text = "Chunk 1" } });

        int callCount = 0;
        _llmServiceMock.Setup(x => x.GenerateAsync(It.IsAny<LlmRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((LlmRequest req, CancellationToken _) => 
            {
                callCount++;
                if (callCount == 1) 
                {
                    // Batch 1: Success (15 questions)
                    var mockQuestions = Enumerable.Range(0, 15).Select(i => new GeneratedQuestionDto 
                    { 
                        Text = "Mock Question", 
                        Type = "MultipleChoice", 
                        Difficulty = "Medium", 
                        SourceChunkIds = new List<string> { chunkId }, 
                        Options = new List<GeneratedQuestionOptionDto> { new() { Text = "A" }, new() { Text = "B" } },
                        CorrectAnswerIndex = 0
                    }).ToList();
                    return new LlmResponse { Content = JsonSerializer.Serialize(new GeneratedExamDto { Status = "success", Questions = mockQuestions }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }) };
                }
                else 
                {
                    // Batch 2: Hallucination (Malformed JSON)
                    return new LlmResponse { Content = "I am an AI and I forgot to output JSON." };
                }
            });

        // Act
        await _sut.ProcessGenerationAsync(generationId, request);

        // Assert
        // We expect CompletedWithWarning because we got 15/30 questions
        Assert.Equal(GenerationStatus.CompletedWithWarning, generation.Status);
        
        // Ensure the valid questions were still saved
        _examRepoMock.Verify(x => x.AddAsync(It.Is<Exam>(e => e.Questions.Count == 15), It.IsAny<CancellationToken>()), Times.Once);
    }
}
