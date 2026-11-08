using AIKnowledgeAssistant.Application.DTOs;

namespace AIKnowledgeAssistant.Application.Interfaces;

public interface IRetrievalService
{
    Task<List<RetrievedChunk>> RetrieveRelevantChunksAsync(Guid organizationId, string query, int topK = 4);
}
