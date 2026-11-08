namespace AIKnowledgeAssistant.Application.DTOs;

public class DocumentDto
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string FormattedSize { get; set; } = string.Empty;
    public int PageCount { get; set; }
    public int ChunkCount { get; set; }
    public DateTime UploadedAt { get; set; }
}

public class DocumentUploadLimits
{
    public const int MaxDocumentsPerOrganization = 10;
    public const int MaxPagesPerDocument = 20;
    public const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10 MB
}

public class UploadDocumentResult
{
    public Guid DocumentId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public int PageCount { get; set; }
    public int ChunkCount { get; set; }
}
