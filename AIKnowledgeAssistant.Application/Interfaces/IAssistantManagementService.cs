using AIKnowledgeAssistant.Application.Common;
using AIKnowledgeAssistant.Application.DTOs;

namespace AIKnowledgeAssistant.Application.Interfaces;

public interface IAssistantManagementService
{
    Task<Result<string>> RegenerateAssistantTokenAsync(Guid organizationId);
    Task<Result<DashboardStatsDto>> GetDashboardStatsAsync(Guid organizationId, string baseUrl);
}
