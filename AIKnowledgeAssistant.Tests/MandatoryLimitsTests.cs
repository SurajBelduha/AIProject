using AIKnowledgeAssistant.Application.DTOs;
using AIKnowledgeAssistant.Domain.Entities;
using AIKnowledgeAssistant.Infrastructure.Services;

namespace AIKnowledgeAssistant.Tests;

public class MandatoryLimitsTests
{
    [Fact]
    public async Task Upload_RejectsFile_Exceeding10MbLimit()
    {
        using var context = await TestDbHelper.CreateInMemoryDbContextAsync();
        var parser = new PdfParserService();
        var kbService = new KnowledgeBaseService(context, parser);

        var orgId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var largeSize = 11 * 1024 * 1024; // 11 MB

        using var dummyStream = new MemoryStream(new byte[100]);
        var result = await kbService.UploadDocumentAsync(orgId, dummyStream, "large_policy.pdf", largeSize);

        Assert.False(result.IsSuccess);
        Assert.Contains("10 MB", result.Error);
    }

    [Fact]
    public async Task Upload_RejectsNonPdfFile()
    {
        using var context = await TestDbHelper.CreateInMemoryDbContextAsync();
        var parser = new PdfParserService();
        var kbService = new KnowledgeBaseService(context, parser);

        var orgId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        using var dummyStream = new MemoryStream(new byte[100]);
        var result = await kbService.UploadDocumentAsync(orgId, dummyStream, "document.docx", 5000);

        Assert.False(result.IsSuccess);
        Assert.Contains("Only PDF documents", result.Error);
    }

    [Fact]
    public async Task Upload_RejectsWhenOrganizationReaches10DocumentLimit()
    {
        using var context = await TestDbHelper.CreateInMemoryDbContextAsync();
        var parser = new PdfParserService();
        var kbService = new KnowledgeBaseService(context, parser);

        var orgId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        // Seed up to 10 documents for this organization
        var currentDocsCount = context.Documents.Count(d => d.OrganizationId == orgId);
        for (int i = currentDocsCount; i < DocumentUploadLimits.MaxDocumentsPerOrganization; i++)
        {
            context.Documents.Add(new Document
            {
                Id = Guid.NewGuid(),
                OrganizationId = orgId,
                FileName = $"seeded_doc_{i}.pdf",
                OriginalFileName = $"seeded_doc_{i}.pdf",
                FileSizeBytes = 1000,
                PageCount = 1
            });
        }
        await context.SaveChangesAsync();

        Assert.Equal(10, context.Documents.Count(d => d.OrganizationId == orgId));

        // Attempting to upload the 11th document
        using var dummyStream = new MemoryStream(new byte[100]);
        var result = await kbService.UploadDocumentAsync(orgId, dummyStream, "eleventh_document.pdf", 5000);

        Assert.False(result.IsSuccess);
        Assert.Contains("10 PDF documents", result.Error);
    }
}
