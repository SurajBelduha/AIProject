using System.Security.Claims;
using AIKnowledgeAssistant.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIKnowledgeAssistant.Web.Controllers;

[Authorize]
public class DashboardController : Controller
{
    private readonly IAssistantManagementService _assistantService;

    public DashboardController(IAssistantManagementService assistantService)
    {
        _assistantService = assistantService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var orgId = GetCurrentOrganizationId();
        if (!orgId.HasValue)
        {
            return RedirectToAction("Login", "Account");
        }

        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var statsResult = await _assistantService.GetDashboardStatsAsync(orgId.Value, baseUrl);

        if (!statsResult.IsSuccess || statsResult.Value == null)
        {
            TempData["ErrorMessage"] = statsResult.Error ?? "Failed to load dashboard data.";
            return View();
        }

        return View(statsResult.Value);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RegenerateToken()
    {
        var orgId = GetCurrentOrganizationId();
        if (!orgId.HasValue)
        {
            return RedirectToAction("Login", "Account");
        }

        var result = await _assistantService.RegenerateAssistantTokenAsync(orgId.Value);
        if (result.IsSuccess)
        {
            TempData["SuccessMessage"] = "Assistant token regenerated successfully. Previous embed tokens have been invalidated.";
        }
        else
        {
            TempData["ErrorMessage"] = result.Error ?? "Failed to regenerate token.";
        }

        return RedirectToAction(nameof(Index));
    }

    private Guid? GetCurrentOrganizationId()
    {
        var orgClaim = User.FindFirst("OrganizationId")?.Value;
        if (Guid.TryParse(orgClaim, out var orgId))
        {
            return orgId;
        }
        return null;
    }
}
