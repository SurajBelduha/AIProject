using System;
using System.Collections.Generic;

namespace AIKnowledgeAssistant.Web.Models;

public partial class Assistant
{
    public Guid Id { get; set; }

    public Guid OrganizationId { get; set; }

    public string Token { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Organization Organization { get; set; } = null!;
}
