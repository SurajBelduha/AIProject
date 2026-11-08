using AIKnowledgeAssistant.Domain.Enums;

namespace AIKnowledgeAssistant.Application.DTOs;

public class ExtractedPage
{
    public int PageNumber { get; set; }
    public string Text { get; set; } = string.Empty;
}

public class ExtractedPdfResult
{
    public int TotalPages { get; set; }
    public List<ExtractedPage> Pages { get; set; } = new();
}

public class RetrievedChunk
{
    public Guid ChunkId { get; set; }
    public Guid DocumentId { get; set; }
    public string DocumentName { get; set; } = string.Empty;
    public int PageNumber { get; set; }
    public string Content { get; set; } = string.Empty;
    public double RelevanceScore { get; set; }
}

public class ConditionRequirement
{
    public string ConditionId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ParameterName { get; set; } = string.Empty;
    public string Operator { get; set; } = ">="; // >=, <=, ==, !=, in
    public string ExpectedValue { get; set; } = string.Empty;
    public string ExtractedValue { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class QuestionDecompositionResult
{
    public QuestionIntent Intent { get; set; }
    public string IntentReasoning { get; set; } = string.Empty;
    public string TargetPolicyCategory { get; set; } = string.Empty;
    public bool RequiresStructuredDecisions { get; set; }
    public Dictionary<string, string> ExtractedEntities { get; set; } = new();
    public List<ConditionRequirement> ConditionsToEvaluate { get; set; } = new();
    public List<string> FocusKeywords { get; set; } = new();
    public bool ContextIsSufficient { get; set; } = true;
}
