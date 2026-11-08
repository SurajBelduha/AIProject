using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace AIKnowledgeAssistant.Web.Models;

public partial class AiknowledgeAssistantContext : DbContext
{
    public AiknowledgeAssistantContext()
    {
    }

    public AiknowledgeAssistantContext(DbContextOptions<AiknowledgeAssistantContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Assistant> Assistants { get; set; }

    public virtual DbSet<Document> Documents { get; set; }

    public virtual DbSet<DocumentChunk> DocumentChunks { get; set; }

    public virtual DbSet<Organization> Organizations { get; set; }

    public virtual DbSet<UsageLog> UsageLogs { get; set; }

    public virtual DbSet<User> Users { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=LAPTOP-BT9E70FJ\\SQLEXPRESS;Database=AIKnowledgeAssistant;MultipleActiveResultSets=True;Trusted_Connection=True;TrustServerCertificate=True;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Assistant>(entity =>
        {
            entity.HasIndex(e => e.OrganizationId, "IX_Assistants_OrganizationId").IsUnique();

            entity.HasIndex(e => e.Token, "IX_Assistants_Token").IsUnique();

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Name).HasMaxLength(200);
            entity.Property(e => e.Token).HasMaxLength(128);

            entity.HasOne(d => d.Organization).WithOne(p => p.Assistant).HasForeignKey<Assistant>(d => d.OrganizationId);
        });

        modelBuilder.Entity<Document>(entity =>
        {
            entity.HasIndex(e => new { e.OrganizationId, e.OriginalFileName }, "IX_Documents_OrganizationId_OriginalFileName");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.FileName).HasMaxLength(260);
            entity.Property(e => e.OriginalFileName).HasMaxLength(260);

            entity.HasOne(d => d.Organization).WithMany(p => p.Documents).HasForeignKey(d => d.OrganizationId);
        });

        modelBuilder.Entity<DocumentChunk>(entity =>
        {
            entity.HasIndex(e => e.DocumentId, "IX_DocumentChunks_DocumentId");

            entity.HasIndex(e => e.OrganizationId, "IX_DocumentChunks_OrganizationId");

            entity.HasIndex(e => new { e.OrganizationId, e.DocumentId }, "IX_DocumentChunks_OrganizationId_DocumentId");

            entity.Property(e => e.Id).ValueGeneratedNever();

            entity.HasOne(d => d.Document).WithMany(p => p.DocumentChunks).HasForeignKey(d => d.DocumentId);
        });

        modelBuilder.Entity<Organization>(entity =>
        {
            entity.HasIndex(e => e.Slug, "IX_Organizations_Slug").IsUnique();

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Name).HasMaxLength(200);
            entity.Property(e => e.Slug).HasMaxLength(200);
        });

        modelBuilder.Entity<UsageLog>(entity =>
        {
            entity.HasIndex(e => e.OrganizationId, "IX_UsageLogs_OrganizationId");

            entity.HasIndex(e => e.Timestamp, "IX_UsageLogs_Timestamp");

            entity.Property(e => e.Id).ValueGeneratedNever();

            entity.HasOne(d => d.Organization).WithMany(p => p.UsageLogs).HasForeignKey(d => d.OrganizationId);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(e => e.Email, "IX_Users_Email").IsUnique();

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Email).HasMaxLength(256);
            entity.Property(e => e.FullName).HasMaxLength(200);

            entity.HasOne(d => d.Organization).WithMany(p => p.Users).HasForeignKey(d => d.OrganizationId);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
