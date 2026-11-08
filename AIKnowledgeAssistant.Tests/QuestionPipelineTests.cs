using AIKnowledgeAssistant.Domain.Enums;
using AIKnowledgeAssistant.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace AIKnowledgeAssistant.Tests;

public class QuestionPipelineTests
{
    private async Task<QuestionPipelineService> CreatePipelineAsync()
    {
        var context = await TestDbHelper.CreateInMemoryDbContextAsync();
        var retrievalService = new HybridRetrievalService(context);
        var decisionEngine = new System1DecisionEngine();
        var llmService = TestDbHelper.CreateTestLlmService();
        return new QuestionPipelineService(context, retrievalService, llmService, decisionEngine);
    }

    // -------------------------------------------------------------
    // Test Questions for Organization 1: Apex University
    // -------------------------------------------------------------

    [Fact]
    public async Task Q1_DirectInformation_Apex_MinimumGpaForTierA()
    {
        var pipeline = await CreatePipelineAsync();
        var res = await pipeline.ProcessQuestionAsync("ast_live_apex_univ_8f93e", "What is the minimum cumulative GPA required for Tier A Merit Honors?");

        Assert.True(res.IsSuccess);
        Assert.NotNull(res.Value);
        Assert.True(res.Value.IsGrounded);
        Assert.Contains("3.85", res.Value.Answer);
        Assert.Contains(res.Value.References, r => r.FormattedCitation.Contains("Scholarship_and_Honors_Rules.pdf"));
    }

    [Fact]
    public async Task Q2_PolicyQuery_Apex_FreshmenEligibility()
    {
        var pipeline = await CreatePipelineAsync();
        var res = await pipeline.ProcessQuestionAsync("ast_live_apex_univ_8f93e", "Are first-year freshman students eligible for university merit scholarships?");

        Assert.True(res.IsSuccess);
        Assert.NotNull(res.Value);
        Assert.True(res.Value.IsGrounded);
        Assert.Contains("Sophomore", res.Value.Answer);
        Assert.Contains("Scholarship_and_Honors_Rules.pdf — Page 2", res.Value.Answer);
    }

    [Fact]
    public async Task Q3_ProceduralQuery_Apex_GradeAppealTimeline()
    {
        var pipeline = await CreatePipelineAsync();
        var res = await pipeline.ProcessQuestionAsync("ast_live_apex_univ_8f93e", "What is the procedure and timeline for filing a formal grade appeal?");

        Assert.True(res.IsSuccess);
        Assert.NotNull(res.Value);
        Assert.True(res.Value.IsGrounded);
        Assert.Contains("14", res.Value.Answer);
        Assert.Contains("Academic_Integrity_and_Grading.pdf — Page 2", res.Value.Answer);
    }

    [Fact]
    public async Task Q4_MultiCondition_Apex_JuniorGpa37WithSportsScholarship()
    {
        var pipeline = await CreatePipelineAsync();
        var res = await pipeline.ProcessQuestionAsync("ast_live_apex_univ_8f93e", 
            "I am a 3rd year student with a 3.7 GPA and an active sports scholarship. Am I eligible for a merit scholarship?");

        Assert.True(res.IsSuccess);
        Assert.NotNull(res.Value);
        Assert.True(res.Value.IsGrounded);
        Assert.NotNull(res.Value.DecisionTrace);

        // Crucial verification: multiple decisions evaluated and overall eligibility is FALSE
        Assert.False(res.Value.DecisionTrace.OverallEligibility);
        Assert.Contains("NOT ELIGIBLE", res.Value.Answer);
        Assert.Contains("Section 4.2", res.Value.Answer);
        Assert.Contains(res.Value.DecisionTrace.Decisions, d => d.Type == DecisionType.Boolean && d.IsPassed == false);
        Assert.Contains(res.Value.References, r => r.PageNumber == 4);
    }

    [Fact]
    public async Task Q5_MultiCondition_Apex_JuniorGpa39WithoutSportsScholarship()
    {
        var pipeline = await CreatePipelineAsync();
        var res = await pipeline.ProcessQuestionAsync("ast_live_apex_univ_8f93e", 
            "I am a 3rd year student with a 3.9 GPA and no athletic stipend. Am I eligible for a merit scholarship?");

        Assert.True(res.IsSuccess);
        Assert.NotNull(res.Value);
        Assert.True(res.Value.IsGrounded);
        Assert.NotNull(res.Value.DecisionTrace);

        // Must be ELIGIBLE
        Assert.True(res.Value.DecisionTrace.OverallEligibility);
        Assert.Contains("ELIGIBLE", res.Value.Answer);
    }

    [Fact]
    public async Task Q6_Comparison_Apex_TierAvsTierB()
    {
        var pipeline = await CreatePipelineAsync();
        var res = await pipeline.ProcessQuestionAsync("ast_live_apex_univ_8f93e", 
            "What is the difference between Tier A and Tier B merit scholarships?");

        Assert.True(res.IsSuccess);
        Assert.NotNull(res.Value);
        Assert.True(res.Value.IsGrounded);
        Assert.Contains("$8,000", res.Value.Answer);
        Assert.Contains("$5,000", res.Value.Answer);
    }

    [Fact]
    public async Task Q7_OutOfKnowledgeBase_Apex_ExternalQuestion()
    {
        var pipeline = await CreatePipelineAsync();
        var res = await pipeline.ProcessQuestionAsync("ast_live_apex_univ_8f93e", "What is the capital of Australia?");

        Assert.True(res.IsSuccess);
        Assert.NotNull(res.Value);
        Assert.False(res.Value.IsGrounded);
        Assert.Equal(QuestionIntent.OutOfKnowledgeBase, res.Value.Intent);
        Assert.Contains("no information", res.Value.Answer, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(res.Value.References);
    }

    // -------------------------------------------------------------
    // Test Questions for Organization 2: FinTech Global
    // -------------------------------------------------------------

    [Fact]
    public async Task Q8_DirectInformation_FinTech_PerDiemMealRate()
    {
        var pipeline = await CreatePipelineAsync();
        var res = await pipeline.ProcessQuestionAsync("ast_live_fintech_glob_4c21b", "What is the daily per diem meal allowance for business travel?");

        Assert.True(res.IsSuccess);
        Assert.NotNull(res.Value);
        Assert.True(res.Value.IsGrounded);
        Assert.Contains("$75.00", res.Value.Answer);
        Assert.Contains("Travel_and_Expense_Guidelines.pdf — Page 1", res.Value.Answer);
    }

    [Fact]
    public async Task Q9_PolicyQuery_FinTech_BusinessClassFlightRules()
    {
        var pipeline = await CreatePipelineAsync();
        var res = await pipeline.ProcessQuestionAsync("ast_live_fintech_glob_4c21b", "When is business class air travel permitted?");

        Assert.True(res.IsSuccess);
        Assert.NotNull(res.Value);
        Assert.True(res.Value.IsGrounded);
        Assert.Contains("8 continuous hours", res.Value.Answer);
        Assert.Contains("Travel_and_Expense_Guidelines.pdf — Page 2", res.Value.Answer);
    }

    [Fact]
    public async Task Q10_MultiCondition_FinTech_RemoteStipendEligibility()
    {
        var pipeline = await CreatePipelineAsync();
        var res = await pipeline.ProcessQuestionAsync("ast_live_fintech_glob_4c21b", 
            "Can I receive the $1200 home office stipend if I have been with the company for 8 months?");

        Assert.True(res.IsSuccess);
        Assert.NotNull(res.Value);
        Assert.True(res.Value.IsGrounded);
        Assert.NotNull(res.Value.DecisionTrace);
        Assert.True(res.Value.DecisionTrace.OverallEligibility);
        Assert.Contains("$1,200", res.Value.Answer);
        Assert.Contains("Remote_Work_and_Equipment_Policy.pdf — Page 3", res.Value.Answer);
    }

    [Fact]
    public async Task Q11_MultiCondition_FinTech_ExpenseOver1000WithoutApproval()
    {
        var pipeline = await CreatePipelineAsync();
        var res = await pipeline.ProcessQuestionAsync("ast_live_fintech_glob_4c21b", 
            "Can I book a $1500 conference flight without manager pre-approval?");

        Assert.True(res.IsSuccess);
        Assert.NotNull(res.Value);
        Assert.True(res.Value.IsGrounded);
        Assert.NotNull(res.Value.DecisionTrace);
        Assert.False(res.Value.DecisionTrace.OverallEligibility);
        Assert.Contains("Pre-Approval", res.Value.Answer);
        Assert.Contains("Travel_and_Expense_Guidelines.pdf — Page 3", res.Value.Answer);
    }

    [Fact]
    public async Task Q12_OutOfKnowledgeBase_FinTech_UnrelatedQuestion()
    {
        var pipeline = await CreatePipelineAsync();
        var res = await pipeline.ProcessQuestionAsync("ast_live_fintech_glob_4c21b", "What is the recipe for chicken parmesan?");

        Assert.True(res.IsSuccess);
        Assert.NotNull(res.Value);
        Assert.False(res.Value.IsGrounded);
        Assert.Contains("no information", res.Value.Answer, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(res.Value.References);
    }
}
