using System;
using System.Collections.Generic;

namespace AIKnowledgeAssistant.Web.Models;

public partial class Document
{
    public Guid Id { get; set; }

    public Guid OrganizationId { get; set; }

    public string FileName { get; set; } = null!;

    public string OriginalFileName { get; set; } = null!;

    public string ContentType { get; set; } = null!;

    public long FileSizeBytes { get; set; }

    public int PageCount { get; set; }

    public string StoragePath { get; set; } = null!;

    public DateTime UploadedAt { get; set; }

    public virtual ICollection<DocumentChunk> DocumentChunks { get; set; } = new List<DocumentChunk>();

    public virtual Organization Organization { get; set; } = null!;
}
