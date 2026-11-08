using AIKnowledgeAssistant.Application.Common;
using AIKnowledgeAssistant.Application.DTOs;

namespace AIKnowledgeAssistant.Application.Interfaces;

public interface IKnowledgeBaseService
{
    Task<Result<UploadDocumentResult>> UploadDocumentAsync(Guid organizationId, Stream fileStream, string originalFileName, long fileSize);
    Task<Result<List<DocumentDto>>> GetOrganizationDocumentsAsync(Guid organizationId);
    Task<Result> DeleteDocumentAsync(Guid organizationId, Guid documentId);
}
