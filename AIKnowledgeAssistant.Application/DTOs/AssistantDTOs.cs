using AIKnowledgeAssistant.Domain.Enums;
using AIKnowledgeAssistant.Domain.ValueObjects;

namespace AIKnowledgeAssistant.Application.DTOs;

public class AskQuestionRequest
{
    public string Question { get; set; } = string.Empty;
    public string? SessionId { get; set; }
}

public class AskQuestionResponse
{
    public string Answer { get; set; } = string.Empty;
    public QuestionIntent Intent { get; set; }
    public string IntentDescription { get; set; } = string.Empty;
    public bool IsGrounded { get; set; }
    public bool RequiresHumanReview { get; set; }
    public double ResponseTimeMs { get; set; }
    public List<ReferenceDto> References { get; set; } = new();
    public DecisionTraceDto? DecisionTrace { get; set; }
}

public class ReferenceDto
{
    public string DocumentName { get; set; } = string.Empty;
    public int PageNumber { get; set; }
    public string Excerpt { get; set; } = string.Empty;
    public double RelevanceScore { get; set; }
    public string FormattedCitation => $"{DocumentName} — Page {PageNumber}";
}

public class DecisionTraceDto
{
    public bool OverallEligibility { get; set; }
    public string Summary { get; set; } = string.Empty;
    public string AppliedPolicyCategory { get; set; } = string.Empty;
    public double SupportScore { get; set; }
    public List<DecisionEvaluation> Decisions { get; set; } = new();
    public List<string> CriticalCitations { get; set; } = new();
}
