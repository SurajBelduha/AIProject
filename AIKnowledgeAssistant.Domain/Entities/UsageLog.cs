using AIKnowledgeAssistant.Domain.Enums;

namespace AIKnowledgeAssistant.Domain.Entities;

public class UsageLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OrganizationId { get; set; }
    public Guid? AssistantId { get; set; }
    public string Question { get; set; } = string.Empty;
    public string Answer { get; set; } = string.Empty;
    public QuestionIntent Intent { get; set; }
    public string? DecisionsAppliedJson { get; set; }
    public bool WasAnswered { get; set; }
    public bool RequiresHumanReview { get; set; }
    public int SourceChunksUsedCount { get; set; }
    public double ResponseTimeMs { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public Organization? Organization { get; set; }
}
