using AIKnowledgeAssistant.Application.Common;
using AIKnowledgeAssistant.Application.DTOs;

namespace AIKnowledgeAssistant.Application.Interfaces;

public interface IPdfParserService
{
    Result<ExtractedPdfResult> ExtractPdf(Stream pdfStream, string fileName);
}
