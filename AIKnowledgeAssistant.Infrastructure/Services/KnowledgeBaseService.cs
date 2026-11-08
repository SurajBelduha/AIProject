using AutoMapper;
using AIKnowledgeAssistant.Application.Common;
using AIKnowledgeAssistant.Application.DTOs;
using AIKnowledgeAssistant.Application.Interfaces;
using AIKnowledgeAssistant.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AIKnowledgeAssistant.Infrastructure.Services;

public class KnowledgeBaseService : IKnowledgeBaseService
{
    private readonly IApplicationDbContext _context;
    private readonly IPdfParserService _pdfParser;
    private readonly IMapper? _mapper;

    public KnowledgeBaseService(IApplicationDbContext context, IPdfParserService pdfParser, IMapper? mapper = null)
    {
        _context = context;
        _pdfParser = pdfParser;
        _mapper = mapper;
    }

    public async Task<Result<UploadDocumentResult>> UploadDocumentAsync(
        Guid organizationId, Stream fileStream, string originalFileName, long fileSize)
    {
        // 1. Mandatory Limit Check: Maximum 10 PDF documents per organization
        var currentDocCount = await _context.Documents.CountAsync(d => d.OrganizationId == organizationId);
        if (currentDocCount >= DocumentUploadLimits.MaxDocumentsPerOrganization)
        {
            return Result<UploadDocumentResult>.Failure(
                $"Upload limit reached: Organization already has {currentDocCount} documents. The mandatory maximum limit is {DocumentUploadLimits.MaxDocumentsPerOrganization} PDF documents. Please delete an older document before uploading a new one.");
        }

        // 2. Mandatory Limit Check: Maximum 10 MB per PDF
        if (fileSize > DocumentUploadLimits.MaxFileSizeBytes)
        {
            var sizeMb = fileSize / (1024.0 * 1024.0);
            return Result<UploadDocumentResult>.Failure(
                $"File size limit exceeded: The uploaded file is {sizeMb:F2} MB. The mandatory maximum limit is 10 MB per PDF.");
        }

        // 3. Extension Validation
        if (!originalFileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
        {
            return Result<UploadDocumentResult>.Failure("Invalid file type: Only PDF documents (.pdf) are permitted.");
        }

        // 4. Parse PDF & check Maximum 20 pages limit
        using var memoryStream = new MemoryStream();
        await fileStream.CopyToAsync(memoryStream);
        memoryStream.Position = 0;

        var parseResult = _pdfParser.ExtractPdf(memoryStream, originalFileName);
        if (!parseResult.IsSuccess || parseResult.Value == null)
        {
            return Result<UploadDocumentResult>.Failure(parseResult.Error ?? "Failed to parse PDF document.");
        }

        var extracted = parseResult.Value;
        if (extracted.TotalPages == 0 || extracted.Pages.All(p => string.IsNullOrWhiteSpace(p.Text)))
        {
            return Result<UploadDocumentResult>.Failure(
                "The uploaded PDF contains no readable text. Scanned images without OCR are not supported.");
        }

        // 5. Create Document record
        var document = new Document
        {
            OrganizationId = organizationId,
            FileName = $"{Guid.NewGuid():N}_{Path.GetFileName(originalFileName)}",
            OriginalFileName = Path.GetFileName(originalFileName),
            ContentType = "application/pdf",
            FileSizeBytes = fileSize,
            PageCount = extracted.TotalPages,
            StoragePath = "database_indexed",
            UploadedAt = DateTime.UtcNow
        };
        _context.Documents.Add(document);

        // 6. Chunking with overlap, retaining exact PageNumber & OrganizationId
        var chunks = ChunkExtractedDocument(document.Id, organizationId, extracted);
        foreach (var chunk in chunks)
        {
            _context.DocumentChunks.Add(chunk);
        }

        await _context.SaveChangesAsync();

        return Result<UploadDocumentResult>.Success(new UploadDocumentResult
        {
            DocumentId = document.Id,
            FileName = document.OriginalFileName,
            PageCount = document.PageCount,
            ChunkCount = chunks.Count
        });
    }

    public async Task<Result<List<DocumentDto>>> GetOrganizationDocumentsAsync(Guid organizationId)
    {
        var docs = await _context.Documents
            .Include(d => d.Chunks)
            .Where(d => d.OrganizationId == organizationId)
            .OrderByDescending(d => d.UploadedAt)
            .ToListAsync();

        var dtos = _mapper != null
            ? _mapper.Map<List<DocumentDto>>(docs)
            : docs.Select(d => new DocumentDto
            {
                Id = d.Id,
                OrganizationId = d.OrganizationId,
                FileName = d.FileName,
                OriginalFileName = d.OriginalFileName,
                FileSizeBytes = d.FileSizeBytes,
                FormattedSize = FormatBytes(d.FileSizeBytes),
                PageCount = d.PageCount,
                ChunkCount = d.Chunks.Count,
                UploadedAt = d.UploadedAt
            }).ToList();

        return Result<List<DocumentDto>>.Success(dtos);
    }

    public async Task<Result> DeleteDocumentAsync(Guid organizationId, Guid documentId)
    {
        var document = await _context.Documents
            .FirstOrDefaultAsync(d => d.Id == documentId && d.OrganizationId == organizationId);

        if (document == null)
        {
            return Result.Failure("Document not found or does not belong to this organization.", 404);
        }

        _context.Documents.Remove(document);
        await _context.SaveChangesAsync();

        return Result.Success();
    }

    private static List<DocumentChunk> ChunkExtractedDocument(
        Guid documentId, Guid organizationId, ExtractedPdfResult extracted)
    {
        var chunks = new List<DocumentChunk>();
        var chunkIndex = 0;

        foreach (var page in extracted.Pages)
        {
            if (string.IsNullOrWhiteSpace(page.Text)) continue;

            // Split into paragraphs or sections
            var paragraphs = page.Text.Split(new[] { "\r\n\r\n", "\n\n", "\r\r" }, StringSplitOptions.RemoveEmptyEntries);

            var currentChunk = "";
            foreach (var para in paragraphs)
            {
                var cleanPara = para.Trim();
                if (string.IsNullOrWhiteSpace(cleanPara)) continue;

                if (currentChunk.Length + cleanPara.Length > 650 && currentChunk.Length > 0)
                {
                    chunks.Add(new DocumentChunk
                    {
                        DocumentId = documentId,
                        OrganizationId = organizationId,
                        ChunkIndex = chunkIndex++,
                        PageNumber = page.PageNumber,
                        Content = currentChunk.Trim()
                    });

                    // Keep a 100-character trailing overlap for semantic continuity
                    var overlap = currentChunk.Length > 100
                        ? currentChunk.Substring(currentChunk.Length - 100)
                        : "";
                    currentChunk = overlap + " " + cleanPara;
                }
                else
                {
                    currentChunk += (currentChunk.Length > 0 ? " " : "") + cleanPara;
                }
            }

            if (!string.IsNullOrWhiteSpace(currentChunk))
            {
                chunks.Add(new DocumentChunk
                {
                    DocumentId = documentId,
                    OrganizationId = organizationId,
                    ChunkIndex = chunkIndex++,
                    PageNumber = page.PageNumber,
                    Content = currentChunk.Trim()
                });
            }
        }

        return chunks;
    }

    private static string FormatBytes(long bytes)
    {
        string[] suffixes = { "B", "KB", "MB", "GB" };
        int i = 0;
        double dblBytes = bytes;
        while (dblBytes >= 1024 && i < suffixes.Length - 1)
        {
            dblBytes /= 1024;
            i++;
        }
        return $"{dblBytes:0.##} {suffixes[i]}";
    }
}
