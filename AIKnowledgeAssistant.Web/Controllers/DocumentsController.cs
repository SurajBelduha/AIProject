using System.Security.Claims;
using AIKnowledgeAssistant.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIKnowledgeAssistant.Web.Controllers;

[Authorize]
public class DocumentsController : Controller
{
    private readonly IKnowledgeBaseService _knowledgeBaseService;

    public DocumentsController(IKnowledgeBaseService knowledgeBaseService)
    {
        _knowledgeBaseService = knowledgeBaseService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var orgId = GetCurrentOrganizationId();
        if (!orgId.HasValue) return RedirectToAction("Login", "Account");

        var result = await _knowledgeBaseService.GetOrganizationDocumentsAsync(orgId.Value);
        return View(result.Value ?? new());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Upload(IFormFile? file)
    {
        var orgId = GetCurrentOrganizationId();
        if (!orgId.HasValue) return RedirectToAction("Login", "Account");

        if (file == null || file.Length == 0)
        {
            TempData["ErrorMessage"] = "Please select a PDF document to upload.";
            return RedirectToAction(nameof(Index));
        }

        using var stream = file.OpenReadStream();
        var result = await _knowledgeBaseService.UploadDocumentAsync(
            orgId.Value, stream, file.FileName, file.Length);

        if (!result.IsSuccess)
        {
            TempData["ErrorMessage"] = result.Error;
        }
        else
        {
            var res = result.Value!;
            TempData["SuccessMessage"] = $"Document '{res.FileName}' successfully processed and indexed ({res.PageCount} pages, {res.ChunkCount} searchable chunks).";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        var orgId = GetCurrentOrganizationId();
        if (!orgId.HasValue) return RedirectToAction("Login", "Account");

        var result = await _knowledgeBaseService.DeleteDocumentAsync(orgId.Value, id);
        if (result.IsSuccess)
        {
            TempData["SuccessMessage"] = "Document and indexed knowledge chunks deleted successfully.";
        }
        else
        {
            TempData["ErrorMessage"] = result.Error ?? "Failed to delete document.";
        }

        return RedirectToAction(nameof(Index));
    }

    private Guid? GetCurrentOrganizationId()
    {
        var orgClaim = User.FindFirst("OrganizationId")?.Value;
        if (Guid.TryParse(orgClaim, out var orgId)) return orgId;
        return null;
    }
}
