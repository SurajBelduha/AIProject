using AIKnowledgeAssistant.Application.DTOs;
using AIKnowledgeAssistant.Domain.Enums;
using AIKnowledgeAssistant.Infrastructure.Services;

namespace AIKnowledgeAssistant.Tests;

public class System1DecisionEngineTests
{
    private readonly System1DecisionEngine _engine = new();

    [Fact]
    public async Task EvaluateDecisions_MultiConditionScenario_ReturnsIneligibleWhenConditionFails()
    {
        // Scenario from doc: Student with GPA 3.7, Year 3, and active sports scholarship
        var decomposition = new QuestionDecompositionResult
        {
            Intent = QuestionIntent.MultiConditionDecision,
            TargetPolicyCategory = "Merit_Scholarship_and_Honors",
            RequiresStructuredDecisions = true,
            ConditionsToEvaluate = new List<ConditionRequirement>
            {
                new()
                {
                    ConditionId = "MinimumGpaRequirement",
                    Name = "Cumulative GPA Threshold",
                    ParameterName = "gpa",
                    Operator = ">=",
                    ExpectedValue = "3.5",
                    ExtractedValue = "3.7"
                },
                new()
                {
                    ConditionId = "AcademicYearRequirement",
                    Name = "Minimum Standing / Academic Year",
                    ParameterName = "academic_year",
                    Operator = ">=",
                    ExpectedValue = "2",
                    ExtractedValue = "3"
                },
                new()
                {
                    ConditionId = "StackingProhibitionRule",
                    Name = "Athletic Award Stacking Exclusion (Section 4.2)",
                    ParameterName = "sports_scholarship",
                    Operator = "==",
                    ExpectedValue = "false",
                    ExtractedValue = "true"
                }
            }
        };

        var context = new List<RetrievedChunk>
        {
            new()
            {
                DocumentName = "Scholarship_and_Honors_Rules.pdf",
                PageNumber = 4,
                Content = "Section 4.2 Stacking Restrictions: University Merit Scholarships CANNOT be combined or stacked with any active athletic sports grant stipends."
            }
        };

        var result = await _engine.EvaluateDecisionsAsync(decomposition, context);

        // Combined result must be Ineligible
        Assert.False(result.OverallEligibility);
        Assert.Contains("INELIGIBLE", result.Summary);
        Assert.Contains("StackingProhibitionRule failed", result.Summary);

        // Verify individual decisions
        var gpaDec = result.Decisions.First(d => d.Name == "MinimumGpaRequirement");
        Assert.True(gpaDec.IsPassed);

        var yearDec = result.Decisions.First(d => d.Name == "AcademicYearRequirement");
        Assert.True(yearDec.IsPassed);

        var sportsDec = result.Decisions.First(d => d.Name == "StackingProhibitionRule");
        Assert.False(sportsDec.IsPassed);

        // Verify Score Decision
        var scoreDec = result.Decisions.First(d => d.Type == DecisionType.Score);
        Assert.NotNull(scoreDec.Score);
        Assert.Equal(2.0 / 3.0, scoreDec.Score.Value, precision: 2); // 2 of 3 passed = 66.7%
    }

    [Fact]
    public async Task EvaluateDecisions_AllConditionsSatisfied_ReturnsEligible()
    {
        var decomposition = new QuestionDecompositionResult
        {
            Intent = QuestionIntent.MultiConditionDecision,
            TargetPolicyCategory = "Merit_Scholarship_and_Honors",
            RequiresStructuredDecisions = true,
            ConditionsToEvaluate = new List<ConditionRequirement>
            {
                new()
                {
                    ConditionId = "MinimumGpaRequirement",
                    Name = "GPA Threshold",
                    ParameterName = "gpa",
                    Operator = ">=",
                    ExpectedValue = "3.5",
                    ExtractedValue = "3.9"
                },
                new()
                {
                    ConditionId = "AcademicYearRequirement",
                    Name = "Year Standing",
                    ParameterName = "academic_year",
                    Operator = ">=",
                    ExpectedValue = "2",
                    ExtractedValue = "3"
                }
            }
        };

        var context = new List<RetrievedChunk>
        {
            new() { DocumentName = "Scholarship_Rules.pdf", PageNumber = 2, Content = "Sophomores with GPA >= 3.5 qualify." }
        };

        var result = await _engine.EvaluateDecisionsAsync(decomposition, context);

        Assert.True(result.OverallEligibility);
        Assert.Contains("APPROVED", result.Summary);
        Assert.Equal(1.0, result.SupportScore);
    }
}
