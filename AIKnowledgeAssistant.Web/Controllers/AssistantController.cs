using AIKnowledgeAssistant.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AIKnowledgeAssistant.Web.Controllers;

public class AssistantController : Controller
{
    private readonly IApplicationDbContext _context;

    public AssistantController(IApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("assistant/{token}")]
    public async Task<IActionResult> Index(string token, bool embed = false)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return NotFound("Assistant token is missing.");
        }

        var assistant = await _context.Assistants
            .Include(a => a.Organization)
            .FirstOrDefaultAsync(a => a.Token == token.Trim());

        if (assistant == null || !assistant.IsActive)
        {
            return NotFound("Assistant not found or inactive.");
        }

        ViewBag.IsEmbed = embed;
        ViewBag.Token = token;
        ViewBag.OrganizationName = assistant.Organization?.Name ?? "Organization";
        ViewBag.AssistantName = assistant.Name;
        ViewBag.AssistantDescription = assistant.Description;

        return View();
    }
}
