using System.Security.Cryptography;
using AIKnowledgeAssistant.Application.Common;
using AIKnowledgeAssistant.Application.DTOs;
using AIKnowledgeAssistant.Application.Interfaces;
using AIKnowledgeAssistant.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AIKnowledgeAssistant.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly IApplicationDbContext _context;

    public AuthService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<AuthResponse>> RegisterAsync(RegisterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
            return Result<AuthResponse>.Failure("Email is required.");
        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 6)
            return Result<AuthResponse>.Failure("Password must be at least 6 characters.");
        if (string.IsNullOrWhiteSpace(request.OrganizationName))
            return Result<AuthResponse>.Failure("Organization name is required.");

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail);
        if (existingUser != null)
        {
            return Result<AuthResponse>.Failure("An account with this email address already exists.");
        }

        var baseSlug = GenerateSlug(request.OrganizationName);
        var slug = baseSlug;
        var slugCount = 1;
        while (await _context.Organizations.AnyAsync(o => o.Slug == slug))
        {
            slug = $"{baseSlug}-{slugCount++}";
        }

        var org = new Organization
        {
            Name = request.OrganizationName.Trim(),
            Slug = slug,
            CreatedAt = DateTime.UtcNow
        };
        _context.Organizations.Add(org);

        var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
        var user = new User
        {
            OrganizationId = org.Id,
            Email = normalizedEmail,
            FullName = string.IsNullOrWhiteSpace(request.FullName) ? request.Email.Split('@')[0] : request.FullName.Trim(),
            PasswordHash = passwordHash,
            CreatedAt = DateTime.UtcNow
        };
        _context.Users.Add(user);

        // Auto-generate Assistant for this Organization
        var assistantToken = GenerateSecureToken();
        var assistant = new Assistant
        {
            OrganizationId = org.Id,
            Token = assistantToken,
            Name = $"{org.Name} Assistant",
            Description = $"Official AI Knowledge & Decision Assistant for {org.Name}",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _context.Assistants.Add(assistant);

        await _context.SaveChangesAsync();

        return Result<AuthResponse>.Success(new AuthResponse
        {
            UserId = user.Id,
            OrganizationId = org.Id,
            OrganizationName = org.Name,
            Email = user.Email,
            FullName = user.FullName,
            Token = assistantToken
        });
    }

    public async Task<Result<AuthResponse>> LoginAsync(LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            return Result<AuthResponse>.Failure("Email and password are required.");

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var user = await _context.Users
            .Include(u => u.Organization)
            .FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail);

        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            return Result<AuthResponse>.Failure("Invalid email or password.");
        }

        var assistant = await _context.Assistants.FirstOrDefaultAsync(a => a.OrganizationId == user.OrganizationId);

        return Result<AuthResponse>.Success(new AuthResponse
        {
            UserId = user.Id,
            OrganizationId = user.OrganizationId,
            OrganizationName = user.Organization?.Name ?? "Organization",
            Email = user.Email,
            FullName = user.FullName,
            Token = assistant?.Token ?? string.Empty
        });
    }

    public async Task<Result<Organization>> GetCurrentOrganizationAsync(Guid organizationId)
    {
        var org = await _context.Organizations
            .Include(o => o.Assistant)
            .FirstOrDefaultAsync(o => o.Id == organizationId);

        if (org == null)
            return Result<Organization>.Failure("Organization not found.", 404);

        return Result<Organization>.Success(org);
    }

    public async Task<Result<User>> GetUserByIdAsync(Guid userId)
    {
        var user = await _context.Users
            .Include(u => u.Organization)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
            return Result<User>.Failure("User not found.", 404);

        return Result<User>.Success(user);
    }

    private static string GenerateSlug(string phrase)
    {
        var str = phrase.ToLowerInvariant();
        var chars = str.Select(c => char.IsLetterOrDigit(c) ? c : '-').ToArray();
        var clean = new string(chars);
        while (clean.Contains("--")) clean = clean.Replace("--", "-");
        return clean.Trim('-');
    }

    public static string GenerateSecureToken()
    {
        var bytes = new byte[18];
        RandomNumberGenerator.Fill(bytes);
        var hex = Convert.ToHexString(bytes).ToLowerInvariant();
        return $"ast_live_{hex}";
    }
}
