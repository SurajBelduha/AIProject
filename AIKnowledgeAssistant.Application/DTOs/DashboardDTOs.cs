namespace AIKnowledgeAssistant.Application.DTOs;

public class DashboardStatsDto
{
    public Guid OrganizationId { get; set; }
    public string OrganizationName { get; set; } = string.Empty;
    public int DocumentCount { get; set; }
    public int MaxDocuments { get; set; } = 10;
    public int TotalPages { get; set; }
    public int TotalQuestions { get; set; }
    public int AnsweredQuestions { get; set; }
    public int HumanReviewCount { get; set; }
    public string AssistantToken { get; set; } = string.Empty;
    public string AssistantPublicUrl { get; set; } = string.Empty;
    public string EmbedIframeCode { get; set; } = string.Empty;
    public string EmbedScriptCode { get; set; } = string.Empty;
    public List<DocumentDto> RecentDocuments { get; set; } = new();
    public List<UsageLogDto> RecentQuestions { get; set; } = new();
}

public class UsageLogDto
{
    public Guid Id { get; set; }
    public string Question { get; set; } = string.Empty;
    public string Answer { get; set; } = string.Empty;
    public string Intent { get; set; } = string.Empty;
    public bool WasAnswered { get; set; }
    public bool RequiresHumanReview { get; set; }
    public double ResponseTimeMs { get; set; }
    public DateTime Timestamp { get; set; }
}
