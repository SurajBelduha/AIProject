using AIKnowledgeAssistant.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AIKnowledgeAssistant.Application.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Organization> Organizations { get; }
    DbSet<User> Users { get; }
    DbSet<Assistant> Assistants { get; }
    DbSet<Document> Documents { get; }
    DbSet<DocumentChunk> DocumentChunks { get; }
    DbSet<UsageLog> UsageLogs { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
