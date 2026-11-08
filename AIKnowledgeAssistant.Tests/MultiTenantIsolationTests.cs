using AIKnowledgeAssistant.Domain.Enums;
using AIKnowledgeAssistant.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace AIKnowledgeAssistant.Tests;

public class MultiTenantIsolationTests
{
    [Fact]
    public async Task RetrievalService_StrictlyIsolatesKnowledgeBase_BetweenTenants()
    {
        using var context = await TestDbHelper.CreateInMemoryDbContextAsync();
        var retrievalService = new HybridRetrievalService(context);

        var apexOrgId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var fintechOrgId = Guid.Parse("22222222-2222-2222-2222-111111111111");

        // Querying FinTech policy topic ("per diem meal allowance") from Apex University
        var apexQueryResults = await retrievalService.RetrieveRelevantChunksAsync(apexOrgId, "per diem meal allowance");
        Assert.Empty(apexQueryResults);

        // Querying FinTech policy topic from FinTech Global
        var fintechQueryResults = await retrievalService.RetrieveRelevantChunksAsync(fintechOrgId, "per diem meal allowance");
        Assert.NotEmpty(fintechQueryResults);
        Assert.All(fintechQueryResults, c => Assert.Contains("Travel_and_Expense", c.DocumentName));
    }

    [Fact]
    public async Task AssistantToken_OnlyRetrievesOwnOrganizationDocuments()
    {
        using var context = await TestDbHelper.CreateInMemoryDbContextAsync();
        var retrievalService = new HybridRetrievalService(context);
        var decisionEngine = new System1DecisionEngine();
        var llmService = TestDbHelper.CreateTestLlmService();
        var pipeline = new QuestionPipelineService(context, retrievalService, llmService, decisionEngine);

        // Apex Assistant Token asks about FinTech's remote work stipend
        var result = await pipeline.ProcessQuestionAsync("ast_live_apex_univ_8f93e", "What is the home office stipend amount?");
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);

        // Because FinTech documents are isolated, Apex's assistant must NOT find FinTech info
        Assert.False(result.Value.IsGrounded);
        Assert.Equal(QuestionIntent.OutOfKnowledgeBase, result.Value.Intent);
        Assert.Contains("no information", result.Value.Answer, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(result.Value.References);
    }

    [Fact]
    public async Task CrossTenantDocumentDelete_IsBlocked()
    {
        using var context = await TestDbHelper.CreateInMemoryDbContextAsync();
        var parser = new PdfParserService();
        var kbService = new KnowledgeBaseService(context, parser);

        var apexOrgId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var fintechDocId = Guid.Parse("22222222-2222-2222-2222-444444444441"); // FinTech doc

        // Apex University attempts to delete FinTech's document
        var deleteResult = await kbService.DeleteDocumentAsync(apexOrgId, fintechDocId);
        Assert.False(deleteResult.IsSuccess);
        Assert.Equal(404, deleteResult.StatusCode);
    }
}
