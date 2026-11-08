using System.Globalization;
using AIKnowledgeAssistant.Application.DTOs;
using AIKnowledgeAssistant.Application.Interfaces;
using AIKnowledgeAssistant.Domain.Enums;
using AIKnowledgeAssistant.Domain.ValueObjects;

namespace AIKnowledgeAssistant.Infrastructure.Services;

public class System1DecisionEngine : ISystem1DecisionEngine
{
    public async Task<CombinedDecisionResult> EvaluateDecisionsAsync(
        QuestionDecompositionResult decomposition, List<RetrievedChunk> context)
    {
        var result = new CombinedDecisionResult
        {
            AppliedPolicyCategory = decomposition.TargetPolicyCategory
        };

        // 1. Classification Decision: Determine question classification & decision mode
        var classificationDecision = EvaluateClassificationDecision(decomposition);
        result.Decisions.Add(classificationDecision);

        // 2. Choice Decision: Select the applicable policy framework or tier
        var choiceDecision = EvaluateChoiceDecision(decomposition, context);
        result.Decisions.Add(choiceDecision);

        // If no structured conditions need evaluation (e.g. pure informational/out-of-domain)
        if (!decomposition.RequiresStructuredDecisions || decomposition.ConditionsToEvaluate.Count == 0)
        {
            result.OverallEligibility = true;
            result.SupportScore = 1.0;
            result.Summary = $"Categorized as '{classificationDecision.ResultValue}'. Policy branch '{choiceDecision.ResultValue}' identified.";
            return result;
        }

        // 3. Boolean Decisions: Independent conditions evaluated in PARALLEL
        var booleanTasks = decomposition.ConditionsToEvaluate
            .Select(cond => Task.Run(() => EvaluateBooleanCondition(cond, context)))
            .ToList();

        var booleanDecisions = await Task.WhenAll(booleanTasks);
        foreach (var boolDec in booleanDecisions)
        {
            result.Decisions.Add(boolDec);
        }

        // 4. Score Decision: Weighted evaluation of policy compliance / support strength
        var scoreDecision = EvaluateScoreDecision(booleanDecisions.ToList(), choiceDecision.ResultValue);
        result.Decisions.Add(scoreDecision);
        result.SupportScore = scoreDecision.Score ?? 0.0;

        // 5. Application Combiner Logic: Synthesize independent decisions into unified outcome
        var allPassed = booleanDecisions.All(b => b.IsPassed == true);
        result.OverallEligibility = allPassed;

        var passedCount = booleanDecisions.Count(b => b.IsPassed == true);
        var failedConditions = booleanDecisions.Where(b => b.IsPassed == false).ToList();

        if (allPassed)
        {
            result.Summary = $"APPROVED / ELIGIBLE: All {booleanDecisions.Length} policy conditions satisfied under category '{choiceDecision.ResultValue}'. Support Score: {result.SupportScore:P0}.";
        }
        else
        {
            var reasons = string.Join("; ", failedConditions.Select(f => $"{f.Name} failed ({f.Rationale})"));
            result.Summary = $"INELIGIBLE / REJECTED: {failedConditions.Count} of {booleanDecisions.Length} conditions failed under '{choiceDecision.ResultValue}'. Reasons: {reasons}. Support Score: {result.SupportScore:P0}.";
        }

        // Collect critical citations from failed or governing rules
        foreach (var d in result.Decisions.Where(d => !string.IsNullOrEmpty(d.EvidenceReference)))
        {
            if (!result.CriticalCitations.Contains(d.EvidenceReference!))
            {
                result.CriticalCitations.Add(d.EvidenceReference!);
            }
        }

        return result;
    }

    private static DecisionEvaluation EvaluateClassificationDecision(QuestionDecompositionResult decomposition)
    {
        return new DecisionEvaluation
        {
            Type = DecisionType.Classification,
            Name = "QuestionIntentClassification",
            Description = "System 1 classification of visitor question type and operational profile",
            ResultValue = decomposition.Intent.ToString(),
            IsPassed = true,
            Rationale = $"Classified as {decomposition.Intent} based on syntactic patterns and entity requirements.",
            Parameters = new Dictionary<string, string>
            {
                { "Intent", decomposition.Intent.ToString() },
                { "RequiresStructuredDecisions", decomposition.RequiresStructuredDecisions.ToString() }
            }
        };
    }

    private static DecisionEvaluation EvaluateChoiceDecision(
        QuestionDecompositionResult decomposition, List<RetrievedChunk> context)
    {
        var category = decomposition.TargetPolicyCategory;
        if (string.IsNullOrWhiteSpace(category))
        {
            category = "GeneralPolicy";
        }

        var relevantChunk = context.FirstOrDefault(c => 
            c.Content.Contains(category, StringComparison.OrdinalIgnoreCase) ||
            c.DocumentName.Contains(category, StringComparison.OrdinalIgnoreCase)) ?? context.FirstOrDefault();

        var citation = relevantChunk != null ? $"{relevantChunk.DocumentName} — Page {relevantChunk.PageNumber}" : null;

        return new DecisionEvaluation
        {
            Type = DecisionType.Choice,
            Name = "PolicyBranchSelection",
            Description = "System 1 choice model selecting the applicable governance policy branch",
            ResultValue = category,
            IsPassed = true,
            Rationale = $"Selected policy framework '{category}' based on matched domain rules and entity context.",
            EvidenceReference = citation,
            Parameters = new Dictionary<string, string>
            {
                { "SelectedCategory", category }
            }
        };
    }

    private static DecisionEvaluation EvaluateBooleanCondition(
        ConditionRequirement req, List<RetrievedChunk> context)
    {
        var passed = false;
        var rationale = "";

        // Normalize parameter
        var param = req.ParameterName.ToLowerInvariant();
        var extractedStr = req.ExtractedValue;
        var expectedStr = req.ExpectedValue;

        // Try numeric comparison
        if (double.TryParse(extractedStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var extractedNum) &&
            double.TryParse(expectedStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var expectedNum))
        {
            switch (req.Operator)
            {
                case ">=":
                    passed = extractedNum >= expectedNum;
                    rationale = passed
                        ? $"Extracted {extractedNum} satisfies requirement (>= {expectedNum})."
                        : $"Extracted {extractedNum} fails requirement (must be >= {expectedNum}).";
                    break;
                case "<=":
                    passed = extractedNum <= expectedNum;
                    rationale = passed
                        ? $"Extracted {extractedNum} satisfies cap (<= {expectedNum})."
                        : $"Extracted {extractedNum} exceeds cap (must be <= {expectedNum}).";
                    break;
                case ">":
                    passed = extractedNum > expectedNum;
                    rationale = passed
                        ? $"Extracted {extractedNum} is strictly greater than {expectedNum}."
                        : $"Extracted {extractedNum} is not greater than {expectedNum}.";
                    break;
                case "<":
                    passed = extractedNum < expectedNum;
                    rationale = passed
                        ? $"Extracted {extractedNum} is strictly less than {expectedNum}."
                        : $"Extracted {extractedNum} is not less than {expectedNum}.";
                    break;
                case "==":
                    passed = Math.Abs(extractedNum - expectedNum) < 0.001;
                    rationale = passed ? $"Matches expected value {expectedNum}." : $"Does not match {expectedNum}.";
                    break;
                default:
                    passed = false;
                    rationale = $"Unsupported operator: {req.Operator}";
                    break;
            }
        }
        else
        {
            // Boolean or text match
            if (bool.TryParse(extractedStr, out var extractedBool) && bool.TryParse(expectedStr, out var expectedBool))
            {
                passed = extractedBool == expectedBool;
                rationale = passed
                    ? $"Boolean flag matches requirement ({extractedBool})."
                    : $"Boolean flag does not match requirement (got {extractedBool}, required {expectedBool}).";
            }
            else
            {
                // Fallback string matching
                passed = string.Equals(extractedStr, expectedStr, StringComparison.OrdinalIgnoreCase);
                rationale = passed
                    ? $"Value '{extractedStr}' matches required '{expectedStr}'."
                    : $"Value '{extractedStr}' does not match required '{expectedStr}'.";
            }
        }

        // Find relevant citation in context
        var chunk = context.FirstOrDefault(c => 
            c.Content.Contains(req.ParameterName, StringComparison.OrdinalIgnoreCase) ||
            c.Content.Contains(req.Name, StringComparison.OrdinalIgnoreCase)) ?? context.FirstOrDefault();

        var citation = chunk != null ? $"{chunk.DocumentName} — Page {chunk.PageNumber}" : null;

        return new DecisionEvaluation
        {
            Type = DecisionType.Boolean,
            Name = req.ConditionId,
            Description = req.Name,
            ResultValue = passed ? "Passed" : "Failed",
            IsPassed = passed,
            Rationale = rationale,
            EvidenceReference = citation,
            Parameters = new Dictionary<string, string>
            {
                { "Parameter", req.ParameterName },
                { "ExtractedValue", extractedStr },
                { "RequiredValue", expectedStr },
                { "Operator", req.Operator }
            }
        };
    }

    private static DecisionEvaluation EvaluateScoreDecision(
        List<DecisionEvaluation> booleanDecisions, string policyCategory)
    {
        if (booleanDecisions.Count == 0)
        {
            return new DecisionEvaluation
            {
                Type = DecisionType.Score,
                Name = "PolicyComplianceScore",
                Description = "Overall quantitative compliance score",
                ResultValue = "100%",
                Score = 1.0,
                Rationale = "No conditional constraints present.",
                IsPassed = true
            };
        }

        // Compute weighted score
        var totalPassed = booleanDecisions.Count(b => b.IsPassed == true);
        var scoreRatio = (double)totalPassed / booleanDecisions.Count;
        var percentage = Math.Round(scoreRatio * 100, 1);

        var isAcceptable = scoreRatio >= 1.0; // In policy evaluation, all mandatory gates must pass

        return new DecisionEvaluation
        {
            Type = DecisionType.Score,
            Name = "PolicyComplianceScore",
            Description = "Quantitative rubric evaluating aggregate satisfaction of policy criteria",
            ResultValue = $"{percentage}%",
            Score = scoreRatio,
            IsPassed = isAcceptable,
            Rationale = $"{totalPassed} of {booleanDecisions.Count} conditions met. Score: {percentage}%.",
            Parameters = new Dictionary<string, string>
            {
                { "ConditionsTotal", booleanDecisions.Count.ToString() },
                { "ConditionsPassed", totalPassed.ToString() },
                { "ScorePercentage", $"{percentage}%" }
            }
        };
    }
}
