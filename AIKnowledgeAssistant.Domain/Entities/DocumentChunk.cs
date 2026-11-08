namespace AIKnowledgeAssistant.Domain.Entities;

public class DocumentChunk
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DocumentId { get; set; }
    public Guid OrganizationId { get; set; }
    public int ChunkIndex { get; set; }
    public int PageNumber { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? EmbeddingJson { get; set; }

    public Document? Document { get; set; }
}
