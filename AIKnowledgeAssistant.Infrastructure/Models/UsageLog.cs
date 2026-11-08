using System;
using System.Collections.Generic;

namespace AIKnowledgeAssistant.Infrastructure.Models;

public partial class UsageLog
{
    public Guid Id { get; set; }

    public Guid OrganizationId { get; set; }

    public Guid? AssistantId { get; set; }

    public string Question { get; set; } = null!;

    public string Answer { get; set; } = null!;

    public int Intent { get; set; }

    public string? DecisionsAppliedJson { get; set; }

    public bool WasAnswered { get; set; }

    public bool RequiresHumanReview { get; set; }

    public int SourceChunksUsedCount { get; set; }

    public double ResponseTimeMs { get; set; }

    public DateTime Timestamp { get; set; }

    public virtual Organization Organization { get; set; } = null!;
}
