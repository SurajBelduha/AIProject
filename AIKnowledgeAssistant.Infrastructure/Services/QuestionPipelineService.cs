using System.Diagnostics;
using System.Text.Json;
using AIKnowledgeAssistant.Application.Common;
using AIKnowledgeAssistant.Application.DTOs;
using AIKnowledgeAssistant.Application.Interfaces;
using AIKnowledgeAssistant.Domain.Entities;
using AIKnowledgeAssistant.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace AIKnowledgeAssistant.Infrastructure.Services;

public class QuestionPipelineService : IQuestionPipelineService
{
    private readonly IApplicationDbContext _context;
    private readonly IRetrievalService _retrievalService;
    private readonly ILlmService _llmService;
    private readonly ISystem1DecisionEngine _decisionEngine;

    public QuestionPipelineService(
        IApplicationDbContext context,
        IRetrievalService retrievalService,
        ILlmService llmService,
        ISystem1DecisionEngine decisionEngine)
    {
        _context = context;
        _retrievalService = retrievalService;
        _llmService = llmService;
        _decisionEngine = decisionEngine;
    }

    public async Task<Result<AskQuestionResponse>> ProcessQuestionAsync(
        string assistantToken, string question, string? sessionId = null)
    {
        var stopwatch = Stopwatch.StartNew();

        // 1. Validation & Tenant Organization Identification
        if (string.IsNullOrWhiteSpace(assistantToken))
        {
            return Result<AskQuestionResponse>.Failure("Assistant token is required.", 401);
        }

        if (string.IsNullOrWhiteSpace(question))
        {
            return Result<AskQuestionResponse>.Failure("Question cannot be empty.", 400);
        }

        var assistant = await _context.Assistants
            .Include(a => a.Organization)
            .FirstOrDefaultAsync(a => a.Token == assistantToken.Trim());

        if (assistant == null || !assistant.IsActive)
        {
            return Result<AskQuestionResponse>.Failure("Invalid or inactive assistant token. Access denied.", 401);
        }

        var organizationId = assistant.OrganizationId;

        // 2. Knowledge Retrieval (Enforcing Strict Multi-Tenant Isolation)
        var retrievedChunks = await _retrievalService.RetrieveRelevantChunksAsync(organizationId, question, topK: 4);

        // 3. Question Understanding & Decomposition (LLM / NLU)
        var decomposition = await _llmService.UnderstandAndDecomposeQuestionAsync(question, retrievedChunks);

        // 4. Decision & Reasoning (System 1 Decision Engine: Choice, Boolean, Score, Classification)
        var decisionResult = await _decisionEngine.EvaluateDecisionsAsync(decomposition, retrievedChunks);

        // 5. Answer Generation (LLM Synthesis Grounded in Retrieved Context)
        var answer = await _llmService.SynthesizeGroundedAnswerAsync(question, retrievedChunks, decisionResult);

        stopwatch.Stop();
        var responseTimeMs = stopwatch.ElapsedMilliseconds;

        var isGrounded = retrievedChunks.Count > 0 && retrievedChunks.Any(c => c.RelevanceScore > 0.08);
        var requiresHumanReview = !isGrounded || (decomposition.Intent == QuestionIntent.OutOfKnowledgeBase);

        // 6. Record Usage Audit Log
        var usageLog = new UsageLog
        {
            OrganizationId = organizationId,
            AssistantId = assistant.Id,
            Question = question.Trim(),
            Answer = answer,
            Intent = decomposition.Intent,
            DecisionsAppliedJson = JsonSerializer.Serialize(decisionResult),
            WasAnswered = isGrounded,
            RequiresHumanReview = requiresHumanReview,
            SourceChunksUsedCount = retrievedChunks.Count,
            ResponseTimeMs = responseTimeMs,
            Timestamp = DateTime.UtcNow
        };
        _context.UsageLogs.Add(usageLog);
        await _context.SaveChangesAsync();

        // 7. Format Final Response with References and Decision Trace
        var response = new AskQuestionResponse
        {
            Answer = answer,
            Intent = decomposition.Intent,
            IntentDescription = decomposition.IntentReasoning,
            IsGrounded = isGrounded,
            RequiresHumanReview = requiresHumanReview,
            ResponseTimeMs = responseTimeMs,
            References = retrievedChunks.Select(c => new ReferenceDto
            {
                DocumentName = c.DocumentName,
                PageNumber = c.PageNumber,
                Excerpt = c.Content.Length > 220 ? c.Content.Substring(0, 220) + "..." : c.Content,
                RelevanceScore = c.RelevanceScore
            }).ToList(),
            DecisionTrace = new DecisionTraceDto
            {
                OverallEligibility = decisionResult.OverallEligibility,
                Summary = decisionResult.Summary,
                AppliedPolicyCategory = decisionResult.AppliedPolicyCategory,
                SupportScore = decisionResult.SupportScore,
                Decisions = decisionResult.Decisions,
                CriticalCitations = decisionResult.CriticalCitations
            }
        };

        return Result<AskQuestionResponse>.Success(response);
    }
}
