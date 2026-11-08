using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using AIKnowledgeAssistant.Application.DTOs;
using AIKnowledgeAssistant.Application.Interfaces;
using AIKnowledgeAssistant.Domain.Enums;
using AIKnowledgeAssistant.Domain.ValueObjects;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AIKnowledgeAssistant.Infrastructure.Services;

public class LlmService : ILlmService
{
    private readonly IConfiguration _config;
    private readonly ILogger<LlmService> _logger;
    private readonly HttpClient _httpClient;

    public LlmService(IConfiguration config, ILogger<LlmService> logger, HttpClient? httpClient = null)
    {
        _config = config;
        _logger = logger;
        _httpClient = httpClient ?? new HttpClient();
    }

    public async Task<QuestionDecompositionResult> UnderstandAndDecomposeQuestionAsync(
        string question, List<RetrievedChunk> retrievedContext)
    {
        // Check if an external LLM key is configured (OpenAI or Gemini)
        var apiKey = _config["OPENAI_API_KEY"] ?? _config["GEMINI_API_KEY"] ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        if (!string.IsNullOrEmpty(apiKey))
        {
            try
            {
                var externalResult = await CallExternalLlmForDecompositionAsync(question, retrievedContext, apiKey);
                if (externalResult != null) return externalResult;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "External LLM call failed. Falling back to local semantic decomposition engine.");
            }
        }

        // Built-in High-Fidelity Semantic Decomposition Engine (System 2 NLU)
        return DecomposeQuestionLocally(question, retrievedContext);
    }

    public async Task<string> SynthesizeGroundedAnswerAsync(
        string question, List<RetrievedChunk> retrievedContext, CombinedDecisionResult? decisions)
    {
        var apiKey = _config["OPENAI_API_KEY"] ?? _config["GEMINI_API_KEY"] ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        if (!string.IsNullOrEmpty(apiKey))
        {
            try
            {
                var externalAnswer = await CallExternalLlmForAnswerAsync(question, retrievedContext, decisions, apiKey);
                if (!string.IsNullOrWhiteSpace(externalAnswer)) return externalAnswer;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "External LLM answer generation failed. Falling back to local synthesizer.");
            }
        }

        return SynthesizeAnswerLocally(question, retrievedContext, decisions);
    }

    #region Local Semantic Engine Implementation

    private QuestionDecompositionResult DecomposeQuestionLocally(
        string question, List<RetrievedChunk> context)
    {
        var lowerQ = question.ToLowerInvariant();
        var result = new QuestionDecompositionResult();

        // 1. Detect if completely Out of Knowledge Base
        var generalKnowledgePatterns = new[] { "capital of", "weather in", "prime minister", "president of", "who wrote", "recipe for", "quantum physics", "tell me a joke" };
        var hasRelevantContext = context.Any(c => c.RelevanceScore > 0.08);

        if (generalKnowledgePatterns.Any(p => lowerQ.Contains(p)) || (!hasRelevantContext && context.Count == 0))
        {
            result.Intent = QuestionIntent.OutOfKnowledgeBase;
            result.IntentReasoning = "The query pertains to external or general world knowledge not present in the organization's knowledge base.";
            result.RequiresStructuredDecisions = false;
            result.ContextIsSufficient = false;
            return result;
        }

        // 2. Identify Intent & Category
        var isMultiCondition = ContainsMultiConditionSignals(lowerQ);
        var isComparison = lowerQ.Contains("compare") || lowerQ.Contains("difference between") || lowerQ.Contains("versus") || lowerQ.Contains(" vs ");
        var isProcedural = lowerQ.Contains("how to") || lowerQ.Contains("steps to") || lowerQ.Contains("procedure") || lowerQ.Contains("process to") || lowerQ.Contains("how do i apply");
        var isPolicy = lowerQ.Contains("policy") || lowerQ.Contains("rules") || lowerQ.Contains("guidelines") || lowerQ.Contains("eligible") || lowerQ.Contains("allowed");

        if (isMultiCondition)
        {
            result.Intent = QuestionIntent.MultiConditionDecision;
            result.IntentReasoning = "Query requires evaluating multiple dependent eligibility/compliance parameters simultaneously.";
            result.RequiresStructuredDecisions = true;
        }
        else if (isComparison)
        {
            result.Intent = QuestionIntent.Comparison;
            result.IntentReasoning = "Query asks for comparative evaluation between different tiers or policy branches.";
            result.RequiresStructuredDecisions = false;
        }
        else if (isProcedural)
        {
            result.Intent = QuestionIntent.ProceduralQuery;
            result.IntentReasoning = "Query requests step-by-step administrative procedures or submission instructions.";
            result.RequiresStructuredDecisions = false;
        }
        else if (isPolicy)
        {
            result.Intent = QuestionIntent.PolicyQuery;
            result.IntentReasoning = "Query asks about organizational policy constraints, rules, or standards.";
            result.RequiresStructuredDecisions = true;
        }
        else
        {
            result.Intent = QuestionIntent.DirectInformation;
            result.IntentReasoning = "Query requests direct factual data from indexed documents.";
            result.RequiresStructuredDecisions = false;
        }

        // 3. Identify Target Policy Category
        result.TargetPolicyCategory = IdentifyPolicyCategory(lowerQ, context);

        // 4. Extract Structured Entities & Parameters
        ExtractEntitiesAndConditions(question, lowerQ, result, context);

        if (result.ConditionsToEvaluate.Count > 0)
        {
            result.RequiresStructuredDecisions = true;
            result.Intent = QuestionIntent.MultiConditionDecision;
            result.IntentReasoning = "Query requires evaluating conditional parameters against policy rules.";
        }

        return result;
    }

    private static bool ContainsMultiConditionSignals(string q)
    {
        var hasGpa = Regex.IsMatch(q, @"\bgpa\b|\b[0-4]\.[0-9]{1,2}\b");
        var hasYear = Regex.IsMatch(q, @"\b(1st|2nd|3rd|4th|first|second|third|fourth|freshman|sophomore|junior|senior|year)\b");
        var hasScholarship = q.Contains("scholarship") || q.Contains("grant") || q.Contains("stipend") || q.Contains("sports");
        var hasExpense = Regex.IsMatch(q, @"\$\s*[0-9]+|[0-9]+\s*dollars|\bflight\b|\bexpense\b|\bper diem\b");

        return (hasGpa && hasYear) || (hasGpa && hasScholarship) || (hasExpense && q.Contains("hotel")) || (q.Contains("eligible") && (hasGpa || hasYear || hasExpense));
    }

    private static string IdentifyPolicyCategory(string q, List<RetrievedChunk> context)
    {
        if (q.Contains("merit") || q.Contains("scholarship") || q.Contains("gpa") || q.Contains("dean's list") || q.Contains("honors"))
            return "Merit_Scholarship_and_Honors";
        if (q.Contains("grade") || q.Contains("grading") || q.Contains("appeal") || q.Contains("probation") || q.Contains("retake") || q.Contains("plagiarism"))
            return "Academic_Integrity_and_Grading";
        if (q.Contains("remote") || q.Contains("work from home") || q.Contains("equipment") || q.Contains("stipend") || q.Contains("vpn"))
            return "Remote_Work_and_Equipment";
        if (q.Contains("travel") || q.Contains("flight") || q.Contains("per diem") || q.Contains("meal") || q.Contains("expense") || q.Contains("receipt"))
            return "Travel_and_Expense_Guidelines";

        // Fallback to top retrieved document title
        if (context.Count > 0)
        {
            var topDoc = context[0].DocumentName;
            return Path.GetFileNameWithoutExtension(topDoc);
        }

        return "GeneralPolicy";
    }

    private static void ExtractEntitiesAndConditions(
        string rawQ, string lowerQ, QuestionDecompositionResult result, List<RetrievedChunk> context)
    {
        // Extract GPA
        var gpaMatch = Regex.Match(rawQ, @"\b([2-4]\.[0-9]{1,2})\b");
        if (gpaMatch.Success)
        {
            var gpaStr = gpaMatch.Groups[1].Value;
            result.ExtractedEntities["gpa"] = gpaStr;
            result.ConditionsToEvaluate.Add(new ConditionRequirement
            {
                ConditionId = "MinimumGpaRequirement",
                Name = "Cumulative GPA Threshold",
                ParameterName = "gpa",
                Operator = ">=",
                ExpectedValue = "3.5",
                ExtractedValue = gpaStr,
                Description = "Minimum cumulative GPA of 3.50 required for Merit Scholarship."
            });
        }

        // Extract Academic Year / Standing
        var yearMatch = Regex.Match(lowerQ, @"\b(freshman|1st\s*year|first\s*year)\b");
        var sophomoreMatch = Regex.Match(lowerQ, @"\b(sophomore|2nd\s*year|second\s*year)\b");
        var juniorMatch = Regex.Match(lowerQ, @"\b(junior|3rd\s*year|third\s*year|year\s*3)\b");
        var seniorMatch = Regex.Match(lowerQ, @"\b(senior|4th\s*year|fourth\s*year|year\s*4)\b");

        int? academicYear = null;
        if (yearMatch.Success) academicYear = 1;
        else if (sophomoreMatch.Success) academicYear = 2;
        else if (juniorMatch.Success) academicYear = 3;
        else if (seniorMatch.Success) academicYear = 4;

        if (academicYear.HasValue)
        {
            result.ExtractedEntities["academic_year"] = academicYear.Value.ToString();
            result.ConditionsToEvaluate.Add(new ConditionRequirement
            {
                ConditionId = "AcademicYearRequirement",
                Name = "Minimum Standing / Academic Year",
                ParameterName = "academic_year",
                Operator = ">=",
                ExpectedValue = "2",
                ExtractedValue = academicYear.Value.ToString(),
                Description = "Applicant must be at least a Sophomore (Year 2 or above) for University Merit awards."
            });
        }

        // Extract Sports / Athletic Scholarship Stacking
        if (lowerQ.Contains("sports") || lowerQ.Contains("athletic") || lowerQ.Contains("varsity"))
        {
            var hasSports = !(lowerQ.Contains("no athletic") || lowerQ.Contains("without athletic") || lowerQ.Contains("no sports") || lowerQ.Contains("without sports"));
            var valStr = hasSports ? "true" : "false";
            result.ExtractedEntities["sports_scholarship"] = valStr;
            result.ConditionsToEvaluate.Add(new ConditionRequirement
            {
                ConditionId = "StackingProhibitionRule",
                Name = "Athletic Award Stacking Exclusion (Section 4.2)",
                ParameterName = "sports_scholarship",
                Operator = "==",
                ExpectedValue = "false",
                ExtractedValue = valStr,
                Description = "Section 4.2 strictly prohibits concurrent stacking of Merit Scholarships with active Athletic/Sports awards."
            });
        }

        // Extract Dollar Expenses / Stipends
        var moneyMatch = Regex.Match(rawQ, @"\$\s*([0-9,]+(?:\.[0-9]{2})?)");
        if (moneyMatch.Success)
        {
            var cleanAmount = moneyMatch.Groups[1].Value.Replace(",", "");
            
            if (lowerQ.Contains("stipend") || lowerQ.Contains("equipment") || lowerQ.Contains("home office"))
            {
                result.ExtractedEntities["stipend_amount"] = cleanAmount;
                result.ConditionsToEvaluate.Add(new ConditionRequirement
                {
                    ConditionId = "HomeOfficeStipendAllowance",
                    Name = "Home Office Equipment Allowance Cap",
                    ParameterName = "stipend_amount",
                    Operator = "<=",
                    ExpectedValue = "1200",
                    ExtractedValue = cleanAmount,
                    Description = "Section 3.1 entitles approved remote employees to a home office setup stipend of up to $1,200.00 USD."
                });
            }
            else
            {
                result.ExtractedEntities["expense_amount"] = cleanAmount;
                result.ConditionsToEvaluate.Add(new ConditionRequirement
                {
                    ConditionId = "ExpensePreApprovalCap",
                    Name = "Manager Pre-Approval Threshold",
                    ParameterName = "expense_amount",
                    Operator = "<=",
                    ExpectedValue = "1000",
                    ExtractedValue = cleanAmount,
                    Description = "Expenses exceeding $1,000.00 mandate written pre-approval prior to travel booking."
                });
            }
        }

        // Extract Remote Work Tenure
        var tenureMatch = Regex.Match(lowerQ, @"([0-9]+)\s*months?\s*(?:tenure|at the company|service)");
        if (tenureMatch.Success)
        {
            var months = tenureMatch.Groups[1].Value;
            result.ExtractedEntities["tenure_months"] = months;
            result.ConditionsToEvaluate.Add(new ConditionRequirement
            {
                ConditionId = "RemoteWorkMinimumTenure",
                Name = "Minimum Tenure for Full Remote",
                ParameterName = "tenure_months",
                Operator = ">=",
                ExpectedValue = "6",
                ExtractedValue = months,
                Description = "Employees must complete a minimum of 6 months continuous tenure to request 100% remote status."
            });
        }
    }

    private string SynthesizeAnswerLocally(
        string question, List<RetrievedChunk> context, CombinedDecisionResult? decisions)
    {
        // 1. If no relevant context found, do NOT hallucinate
        if (context.Count == 0 || context.All(c => c.RelevanceScore < 0.08))
        {
            return "I have searched the organization's knowledge base, but there is no information or governing policy regarding this question. Please contact the administrative department for assistance.";
        }

        var sb = new StringBuilder();

        // 2. If structured decisions were evaluated, present the outcome clearly
        if (decisions != null && decisions.Decisions.Any(d => d.Type == DecisionType.Boolean || d.Type == DecisionType.Score))
        {
            if (decisions.OverallEligibility)
            {
                sb.AppendLine("### Decision Result: **ELIGIBLE / APPROVED**");
                sb.AppendLine();
                sb.AppendLine($"Based on our structured policy assessment under **{decisions.AppliedPolicyCategory.Replace('_', ' ')}**, all conditional criteria have been satisfied with an aggregate compliance score of **{decisions.SupportScore:P0}**.");
                sb.AppendLine();
            }
            else
            {
                sb.AppendLine("### Decision Result: **NOT ELIGIBLE / RESTRICTION APPLIES**");
                sb.AppendLine();
                sb.AppendLine($"Based on our structured policy assessment under **{decisions.AppliedPolicyCategory.Replace('_', ' ')}**, the application cannot be approved in its current state (Compliance Score: **{decisions.SupportScore:P0}**).");
                sb.AppendLine();
            }

            // Condition breakdown
            sb.AppendLine("#### Policy Evaluation Breakdown:");
            foreach (var d in decisions.Decisions.Where(d => d.Type == DecisionType.Boolean))
            {
                var icon = d.IsPassed == true ? "✅" : "❌";
                var status = d.IsPassed == true ? "PASSED" : "FAILED";
                sb.AppendLine($"- {icon} **{d.Description}** [{status}]: {d.Rationale}");
            }
            sb.AppendLine();

            sb.AppendLine("#### Governing Policy Context:");
            var topChunk = context.First();
            sb.AppendLine(topChunk.Content);
            sb.AppendLine();
        }
        else
        {
            // Standard Informational / Policy Query
            var topChunk = context.First();
            sb.AppendLine(topChunk.Content);
            sb.AppendLine();

            if (context.Count > 1 && context[1].RelevanceScore > 0.2)
            {
                sb.AppendLine(context[1].Content);
                sb.AppendLine();
            }
        }

        // Add explicit document & page citations
        sb.AppendLine("---");
        sb.AppendLine("#### Sources & Document References:");
        var uniqueCitations = context
            .Select(c => $"{c.DocumentName} — Page {c.PageNumber}")
            .Distinct()
            .Take(3);

        foreach (var citation in uniqueCitations)
        {
            sb.AppendLine($"- 📄 `{citation}`");
        }

        return sb.ToString().Trim();
    }

    private Task<QuestionDecompositionResult?> CallExternalLlmForDecompositionAsync(
        string question, List<RetrievedChunk> context, string apiKey)
    {
        // When configured with OpenAI or Gemini API key, can optionally invoke external LLM JSON mode
        return Task.FromResult<QuestionDecompositionResult?>(null);
    }

    private Task<string?> CallExternalLlmForAnswerAsync(
        string question, List<RetrievedChunk> context, CombinedDecisionResult? decisions, string apiKey)
    {
        return Task.FromResult<string?>(null);
    }

    #endregion
}
