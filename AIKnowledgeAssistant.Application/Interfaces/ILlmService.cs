using AIKnowledgeAssistant.Application.DTOs;
using AIKnowledgeAssistant.Domain.ValueObjects;

namespace AIKnowledgeAssistant.Application.Interfaces;

public interface ILlmService
{
    Task<QuestionDecompositionResult> UnderstandAndDecomposeQuestionAsync(string question, List<RetrievedChunk> retrievedContext);
    Task<string> SynthesizeGroundedAnswerAsync(string question, List<RetrievedChunk> retrievedContext, CombinedDecisionResult? decisions);
}
