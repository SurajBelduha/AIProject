using AIKnowledgeAssistant.Infrastructure.Persistence;
using AIKnowledgeAssistant.Infrastructure.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Primitives;

namespace AIKnowledgeAssistant.Tests;

public class TestConfiguration : IConfiguration
{
    private readonly Dictionary<string, string?> _data = new();

    public string? this[string key]
    {
        get => _data.TryGetValue(key, out var val) ? val : null;
        set => _data[key] = value;
    }

    public IEnumerable<IConfigurationSection> GetChildren() => Enumerable.Empty<IConfigurationSection>();
    public IChangeToken GetReloadToken() => NullChangeToken.Instance;
    public IConfigurationSection GetSection(string key) => null!;
}

internal class NullChangeToken : IChangeToken
{
    public static readonly NullChangeToken Instance = new();
    public bool HasChanged => false;
    public bool ActiveChangeCallbacks => false;
    public IDisposable RegisterChangeCallback(Action<object?> callback, object? state) => NullDisposable.Instance;
}

internal class NullDisposable : IDisposable
{
    public static readonly NullDisposable Instance = new();
    public void Dispose() { }
}

public static class TestDbHelper
{
    public static async Task<ApplicationDbContext> CreateInMemoryDbContextAsync()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;

        var context = new ApplicationDbContext(options);
        await SeedData.InitializeAsync(context);

        return context;
    }

    public static LlmService CreateTestLlmService()
    {
        var config = new TestConfiguration();
        var logger = NullLogger<LlmService>.Instance;
        return new LlmService(config, logger);
    }
}
