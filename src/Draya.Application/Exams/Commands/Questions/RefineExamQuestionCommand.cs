using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Draya.Application.AI;
using Draya.Application.AI.Models;
using Draya.Application.Exams.Services;
using Draya.Application.Materials;
using Draya.Application.Materials.RAG;
using Draya.Domain.Exams;
using Draya.Domain.Materials;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Draya.Application.Exams.Commands.Questions;

public class RefineExamQuestionCommand : IRequest<GeneratedQuestionDto?>
{
    public Guid ExamId { get; set; }
    public Guid QuestionId { get; set; }
    public Guid TeacherId { get; set; }
    public string Instruction { get; set; } = string.Empty;
}

public class RefineExamQuestionCommandHandler : IRequestHandler<RefineExamQuestionCommand, GeneratedQuestionDto?>
{
    private readonly IExamRepository _examRepository;
    private readonly IMaterialRepository _materialRepo;
    private readonly IRetrievalService _retrievalService;
    private readonly ILLMService _llmService;
    private readonly ILogger<RefineExamQuestionCommandHandler> _logger;

    public RefineExamQuestionCommandHandler(
        IExamRepository examRepository,
        IMaterialRepository materialRepo,
        IRetrievalService retrievalService,
        ILLMService llmService,
        ILogger<RefineExamQuestionCommandHandler> logger)
    {
        _examRepository = examRepository;
        _materialRepo = materialRepo;
        _retrievalService = retrievalService;
        _llmService = llmService;
        _logger = logger;
    }

    public async Task<GeneratedQuestionDto?> Handle(RefineExamQuestionCommand request, CancellationToken cancellationToken)
    {
        var exam = await _examRepository.GetByIdAsync(request.ExamId, cancellationToken);
        if (exam == null) return null;

        var question = exam.Questions.FirstOrDefault(q => q.Id == request.QuestionId);
        if (question == null) return null;

        var materialVersionIds = await _materialRepo.GetParsedMaterialVersionIdsBySectionIdAsync(exam.SectionId, cancellationToken);
        if (!materialVersionIds.Any()) return null;

        // Retrieve chunks based on original topic + refinement instruction
        var query = new RetrievalQuery
        {
            MaterialVersionIds = materialVersionIds,
            QueryText = $"{exam.Topic} {request.Instruction}",
            TopK = 15,
            MinScore = 0.5f
        };

        var retrievedChunks = await _retrievalService.SearchAsync(query, cancellationToken);
        if (retrievedChunks.Count == 0) return null;

        var contextBuilder = new System.Text.StringBuilder();
        foreach (var chunk in retrievedChunks)
        {
            contextBuilder.AppendLine($"<chunk id=\"{chunk.ChunkId}\">\n{chunk.Text}\n</chunk>");
        }
        var contextData = contextBuilder.ToString();

        var systemPrompt = $@"You are a strict, helpful AI teacher assistant. Your task is to refine/rewrite an existing exam question according to the teacher's instructions, and output in valid JSON format.
You MUST base your question ONLY on the provided Context Data.
You MUST return an array of sourceChunkIds for the generated question. The sourceChunkIds MUST strictly match the 'id' attributes provided in the Context Data.

# Context Data
{contextData}

# Original Question
Text: {question.Text}
Type: {question.Type}
Difficulty: {question.Difficulty}
Rubric: {question.Rubric}

# Instructions
Refine the original question according to the user's instruction.
You must output a JSON object adhering to this schema:
{{
  ""text"": ""Refined question text"",
  ""type"": ""{question.Type}"", // Unless instructed otherwise, keep the same type
  ""difficulty"": ""{question.Difficulty}"",
  ""sourceChunkIds"": [""uuid""],
  // For MultipleChoice or TrueFalse:
  ""options"": [{{""text"": ""opt1""}}, {{""text"": ""opt2""}}],
  ""correctAnswerIndex"": 0,
  // For FillInTheBlank:
  ""acceptedAnswers"": [""answer1"", ""answer2""],
  // For Essay or ShortAnswer:
  ""rubric"": ""Detailed grading criteria""
}}
";

        var userPrompt = $"Teacher Instruction: {request.Instruction}";

        var llmRequest = new LlmRequest
        {
            Feature = AiFeature.QuestionRefinement,
            SystemPrompt = systemPrompt,
            UserPrompt = userPrompt,
            RequestJsonResponse = true
        };

        var llmResponse = await _llmService.GenerateAsync(llmRequest, cancellationToken);
        var jsonContent = CleanLlmJsonResponse(llmResponse.Content);

        if (string.IsNullOrWhiteSpace(jsonContent)) return null;

        var deserializeOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var qDto = JsonSerializer.Deserialize<GeneratedQuestionDto>(jsonContent, deserializeOptions);
        
        if (qDto == null) return null;

        // Grounding validation
        if (qDto.SourceChunkIds == null || qDto.SourceChunkIds.Count == 0) return null;
        
        var retrievedChunkIds = retrievedChunks.Select(c => c.ChunkId).ToHashSet();
        bool hasValidChunk = qDto.SourceChunkIds.Any(cid => retrievedChunkIds.Contains(cid));
        if (!hasValidChunk) return null;

        // Note: We do NOT automatically apply the refinement. We return the DTO to the frontend.
        // The frontend will show it to the teacher, and if they approve, they call PUT to update.
        return qDto;
    }

    private string CleanLlmJsonResponse(string content)
    {
        if (string.IsNullOrWhiteSpace(content)) return content;
        
        var trimmed = content.Trim();
        if (trimmed.StartsWith("```json", StringComparison.OrdinalIgnoreCase))
        {
            trimmed = trimmed.Substring(7);
        }
        else if (trimmed.StartsWith("```", StringComparison.OrdinalIgnoreCase))
        {
            trimmed = trimmed.Substring(3);
        }
        
        if (trimmed.EndsWith("```", StringComparison.OrdinalIgnoreCase))
        {
            trimmed = trimmed.Substring(0, trimmed.Length - 3);
        }

        return trimmed.Trim();
    }
}
