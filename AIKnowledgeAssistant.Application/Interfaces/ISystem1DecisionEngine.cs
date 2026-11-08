using AIKnowledgeAssistant.Application.DTOs;
using AIKnowledgeAssistant.Domain.ValueObjects;

namespace AIKnowledgeAssistant.Application.Interfaces;

public interface ISystem1DecisionEngine
{
    Task<CombinedDecisionResult> EvaluateDecisionsAsync(QuestionDecompositionResult decomposition, List<RetrievedChunk> context);
}
