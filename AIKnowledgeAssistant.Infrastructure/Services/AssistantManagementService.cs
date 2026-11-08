using AutoMapper;
using AIKnowledgeAssistant.Application.Common;
using AIKnowledgeAssistant.Application.DTOs;
using AIKnowledgeAssistant.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AIKnowledgeAssistant.Infrastructure.Services;

public class AssistantManagementService : IAssistantManagementService
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper? _mapper;

    public AssistantManagementService(IApplicationDbContext context, IMapper? mapper = null)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<Result<string>> RegenerateAssistantTokenAsync(Guid organizationId)
    {
        var assistant = await _context.Assistants
            .FirstOrDefaultAsync(a => a.OrganizationId == organizationId);

        if (assistant == null)
        {
            return Result<string>.Failure("Assistant not found for this organization.", 404);
        }

        var newToken = AuthService.GenerateSecureToken();
        assistant.Token = newToken;
        assistant.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return Result<string>.Success(newToken);
    }

    public async Task<Result<DashboardStatsDto>> GetDashboardStatsAsync(Guid organizationId, string baseUrl)
    {
        var org = await _context.Organizations
            .Include(o => o.Assistant)
            .FirstOrDefaultAsync(o => o.Id == organizationId);

        if (org == null)
        {
            return Result<DashboardStatsDto>.Failure("Organization not found.", 404);
        }

        var docs = await _context.Documents
            .Where(d => d.OrganizationId == organizationId)
            .OrderByDescending(d => d.UploadedAt)
            .ToListAsync();

        var totalPages = docs.Sum(d => d.PageCount);
        var docCount = docs.Count;

        var token = org.Assistant?.Token ?? string.Empty;
        var cleanBaseUrl = baseUrl.TrimEnd('/');
        var publicUrl = $"{cleanBaseUrl}/assistant/{token}";
        var embedIframe = $"<iframe src=\"{cleanBaseUrl}/assistant/{token}?embed=true\" width=\"100%\" height=\"600\" frameborder=\"0\" allow=\"clipboard-write\"></iframe>";
        var embedScript = $"<div id=\"ai-assistant-container\"></div>\n<script src=\"{cleanBaseUrl}/js/assistant-widget.js\" data-token=\"{token}\" data-endpoint=\"{cleanBaseUrl}/api/ask\"></script>";

        var logs = await _context.UsageLogs
            .Where(u => u.OrganizationId == organizationId)
            .OrderByDescending(u => u.Timestamp)
            .Take(10)
            .ToListAsync();

        var totalQuestions = await _context.UsageLogs.CountAsync(u => u.OrganizationId == organizationId);
        var answeredQuestions = await _context.UsageLogs.CountAsync(u => u.OrganizationId == organizationId && u.WasAnswered);
        var reviewNeeded = await _context.UsageLogs.CountAsync(u => u.OrganizationId == organizationId && u.RequiresHumanReview);

        var stats = new DashboardStatsDto
        {
            OrganizationId = org.Id,
            OrganizationName = org.Name,
            DocumentCount = docCount,
            MaxDocuments = DocumentUploadLimits.MaxDocumentsPerOrganization,
            TotalPages = totalPages,
            TotalQuestions = totalQuestions,
            AnsweredQuestions = answeredQuestions,
            HumanReviewCount = reviewNeeded,
            AssistantToken = token,
            AssistantPublicUrl = publicUrl,
            EmbedIframeCode = embedIframe,
            EmbedScriptCode = embedScript,
            RecentDocuments = _mapper != null
                ? _mapper.Map<List<DocumentDto>>(docs.Take(5))
                : docs.Take(5).Select(d => new DocumentDto
                {
                    Id = d.Id,
                    OrganizationId = d.OrganizationId,
                    FileName = d.FileName,
                    OriginalFileName = d.OriginalFileName,
                    FileSizeBytes = d.FileSizeBytes,
                    FormattedSize = $"{d.FileSizeBytes / 1024.0:F1} KB",
                    PageCount = d.PageCount,
                    UploadedAt = d.UploadedAt
                }).ToList(),
            RecentQuestions = _mapper != null
                ? _mapper.Map<List<UsageLogDto>>(logs)
                : logs.Select(l => new UsageLogDto
                {
                    Id = l.Id,
                    Question = l.Question,
                    Answer = l.Answer.Length > 180 ? l.Answer.Substring(0, 180) + "..." : l.Answer,
                    Intent = l.Intent.ToString(),
                    WasAnswered = l.WasAnswered,
                    RequiresHumanReview = l.RequiresHumanReview,
                    ResponseTimeMs = l.ResponseTimeMs,
                    Timestamp = l.Timestamp
                }).ToList()
        };

        return Result<DashboardStatsDto>.Success(stats);
    }
}
