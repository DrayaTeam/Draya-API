# AI Integration in Draya-API

This document provides a comprehensive overview of how Artificial Intelligence (AI) is integrated and utilized within the Draya-API backend project. 

## 1. AI Provider & Infrastructure

### OpenRouter LLM Service
The project interfaces with Large Language Models (LLMs) through **OpenRouter**, an aggregator that allows the application to use various models (like OpenAI, Anthropic, Gemini, etc.) through a unified API.

- **Implementation**: [`OpenRouterLlmService`](file:///e:/SuperTryHard/Grad/Backend/Draya-API/src/Draya.Infrastructure/AI/OpenRouter/OpenRouterLlmService.cs)
- **Role**: Implements the `ILLMService` interface. It handles sending prompts (System and User prompts) to the configured OpenRouter model, manages token configurations (`max_tokens`, `temperature`), and parses the JSON response.
- **JSON Mode**: It explicitly supports requesting JSON-formatted responses (`response_format = { type = "json_object" }`), which is heavily used for structured data generation (like exam questions and grading results).

## 2. Core AI Features

### A. Exam Generation (RAG - Retrieval-Augmented Generation)
The system can automatically generate entire exams based on uploaded classroom materials.

- **Implementation**: [`ExamGenerationService`](file:///e:/SuperTryHard/Grad/Backend/Draya-API/src/Draya.Application/Exams/Services/ExamGenerationService.cs)
- **Process**:
  1. **Retrieval**: Uses a `RetrievalService` to fetch relevant chunks of parsed materials based on the requested exam topic.
  2. **Anonymization**: Anonymizes the Teacher ID before interacting with the LLM to protect privacy.
  3. **Batching**: Groups question generation into batches (e.g., 15 questions per batch) to avoid exceeding token limits.
  4. **Context Injection**: Feeds the retrieved chunks as Context Data (`<chunk id="...">`) to the LLM to ground the generated questions in the actual course material, preventing hallucinations.
  5. **Validation & Parsing**: Generates varied question types (Multiple Choice, True/False, Fill in the Blank, Essay) in strict JSON format. Validates that the generated questions cite the source chunk IDs correctly.
- **Prompting Strategy**: Uses a strict system prompt enforcing difficulty levels, formats, and grounding, while treating user (teacher) instructions as untrusted constraints.

### B. Subjective Exam Grading
The AI acts as an automated teaching assistant to grade subjective student answers based on rubrics.

- **Implementation**: [`ExamGradingService`](file:///e:/SuperTryHard/Grad/Backend/Draya-API/src/Draya.Application/Exams/Services/ExamGradingService.cs)
- **Process**:
  1. **Objective vs. Subjective**: The service handles objective questions deterministically, but forwards subjective/essay questions to the AI.
  2. **Anonymization**: The student's ID is anonymized (`IPiiAnonymizer`) before their answer is sent to the LLM.
  3. **Evaluation**: The LLM is provided with the question type, text, max score, and the ideal rubric.
  4. **Output**: The AI returns a JSON object containing a `score`, a `confidenceScore` (0.0 to 1.0), and a detailed `rationale` explaining the grade.
  5. **Human-in-the-Loop**: If the AI's `confidenceScore` falls below a set threshold (`ConfidenceThreshold = 0.85m`), the system automatically flags the answer as `NeedsTeacherReview`, requiring human validation.

### C. Exam Question Refinement
Teachers can ask the AI to refine or rewrite an existing exam question.

- **Implementation**: `RefineExamQuestionCommand` (in `Draya.Application\Exams\Commands\Questions\RefineExamQuestionCommand.cs`)
- **Process**: Takes an existing question and a set of instructions from the teacher, sending them to the LLM to rewrite the question while preserving its intended format and validity.

## 3. Data Privacy & Security

- **PII Anonymization**: The project includes an `IPiiAnonymizer` service. Whenever user-generated content or actions (like grading a student's answer or generating an exam for a teacher) are sent to the external LLM, the system strips out real User IDs and replaces them with anonymized identifiers.
- **Untrusted Input Handling**: Teacher instructions are wrapped in specific XML tags (`<teacher_instructions>`) and the system prompt explicitly commands the AI to treat them as untrusted constraints that must not override core generation rules.

## 4. Business Logic & Monetization

AI generation costs money (tokens), and the project implements rate-limiting and monetization logic around AI features.

- **Platform Settings**: Configured in `PlatformSetting` entity (`Draya.Domain.Admin`).
  - `AIExamPrice`: The cost associated with generating an AI exam beyond the free quota.
  - `FreeMonthlyAIExamQuota`: A set limit of free AI exam generations provided to users per month.
- **Background Processing**: Both Exam Generation and Exam Grading are handled asynchronously via background task queues (`IExamGradingTaskQueue`, `IExamGenerationTaskQueue`) because LLM calls can be slow and might time out if processed synchronously on the main thread. Events are published to update the UI on the generation/grading progress.
