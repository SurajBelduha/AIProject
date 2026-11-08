using AIKnowledgeAssistant.Domain.Enums;

namespace AIKnowledgeAssistant.Domain.ValueObjects;

public class DecisionEvaluation
{
    public DecisionType Type { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ResultValue { get; set; } = string.Empty;
    public bool? IsPassed { get; set; }
    public double? Score { get; set; }
    public string Rationale { get; set; } = string.Empty;
    public string? EvidenceReference { get; set; }
    public Dictionary<string, string> Parameters { get; set; } = new();
}

public class CombinedDecisionResult
{
    public bool OverallEligibility { get; set; }
    public string Summary { get; set; } = string.Empty;
    public string AppliedPolicyCategory { get; set; } = string.Empty;
    public double SupportScore { get; set; }
    public List<DecisionEvaluation> Decisions { get; set; } = new();
    public List<string> CriticalCitations { get; set; } = new();
}
