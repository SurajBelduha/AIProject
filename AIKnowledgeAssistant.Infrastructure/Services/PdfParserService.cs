using AIKnowledgeAssistant.Application.Common;
using AIKnowledgeAssistant.Application.DTOs;
using AIKnowledgeAssistant.Application.Interfaces;
using UglyToad.PdfPig;

namespace AIKnowledgeAssistant.Infrastructure.Services;

public class PdfParserService : IPdfParserService
{
    public Result<ExtractedPdfResult> ExtractPdf(Stream pdfStream, string fileName)
    {
        if (pdfStream == null || pdfStream.Length == 0)
        {
            return Result<ExtractedPdfResult>.Failure("The uploaded file is empty.");
        }

        try
        {
            using var pdf = PdfDocument.Open(pdfStream);
            
            if (pdf.NumberOfPages > DocumentUploadLimits.MaxPagesPerDocument)
            {
                return Result<ExtractedPdfResult>.Failure(
                    $"Document contains {pdf.NumberOfPages} pages, which exceeds the mandatory limit of {DocumentUploadLimits.MaxPagesPerDocument} pages per PDF.");
            }

            var result = new ExtractedPdfResult
            {
                TotalPages = pdf.NumberOfPages
            };

            foreach (var page in pdf.GetPages())
            {
                var text = page.Text ?? string.Empty;
                result.Pages.Add(new ExtractedPage
                {
                    PageNumber = page.Number,
                    Text = text.Trim()
                });
            }

            return Result<ExtractedPdfResult>.Success(result);
        }
        catch (Exception ex)
        {
            return Result<ExtractedPdfResult>.Failure(
                $"Failed to process PDF '{fileName}': The file is not a valid or readable PDF format. Details: {ex.Message}");
        }
    }
}
