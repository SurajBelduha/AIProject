using AIKnowledgeAssistant.Application.Common;
using AIKnowledgeAssistant.Application.DTOs;

namespace AIKnowledgeAssistant.Application.Interfaces;

public interface IQuestionPipelineService
{
    Task<Result<AskQuestionResponse>> ProcessQuestionAsync(string assistantToken, string question, string? sessionId = null);
}
