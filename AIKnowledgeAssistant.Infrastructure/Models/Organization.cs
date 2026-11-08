using System;
using System.Collections.Generic;

namespace AIKnowledgeAssistant.Infrastructure.Models;

public partial class Organization
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public string Slug { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public virtual Assistant? Assistant { get; set; }

    public virtual ICollection<Document> Documents { get; set; } = new List<Document>();

    public virtual ICollection<UsageLog> UsageLogs { get; set; } = new List<UsageLog>();

    public virtual ICollection<User> Users { get; set; } = new List<User>();
}
