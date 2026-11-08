using AIKnowledgeAssistant.Application;
using AIKnowledgeAssistant.Application.Interfaces;
using AIKnowledgeAssistant.Infrastructure.Persistence;
using AIKnowledgeAssistant.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AIKnowledgeAssistant.Infrastructure;

public static class ServicesConfiguration
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        // 1. Database Context Configuration (SQL Server / SQLite)
        var connectionString = configuration.GetConnectionString("DefaultConnection") 
                               ?? "Data Source=aiknowledge.db";

        if (connectionString.Contains("SQLEXPRESS", StringComparison.OrdinalIgnoreCase) || 
            connectionString.Contains("Server=", StringComparison.OrdinalIgnoreCase))
        {
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(connectionString));
        }
        else
        {
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlite(connectionString));
        }

        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());

        // 2. Application & Domain Services Registration
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IPdfParserService, PdfParserService>();
        services.AddScoped<IKnowledgeBaseService, KnowledgeBaseService>();
        services.AddScoped<IRetrievalService, HybridRetrievalService>();
        services.AddScoped<ISystem1DecisionEngine, System1DecisionEngine>();
        services.AddScoped<ILlmService, LlmService>();
        services.AddScoped<IQuestionPipelineService, QuestionPipelineService>();
        services.AddScoped<IAssistantManagementService, AssistantManagementService>();
        services.AddScoped<HttpClient>();

        return services;
    }

    /// <summary>
    /// Alias for AddInfrastructureServices matching Repository service pattern
    /// </summary>
    public static IServiceCollection AddRepositoryService(this IServiceCollection services, IConfiguration configuration)
    {
        return services.AddInfrastructureServices(configuration);
    }

    /// <summary>
    /// Combined Provider configuration registering AutoMapper, Repository, and Application/Domain Services
    /// </summary>
    public static IServiceCollection AddProviderServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddApplicationServices(configuration);
        services.AddInfrastructureServices(configuration);
        return services;
    }
}
