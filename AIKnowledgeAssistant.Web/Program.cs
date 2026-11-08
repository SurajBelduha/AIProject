using AIKnowledgeAssistant.Application;
using AIKnowledgeAssistant.Infrastructure;
using AIKnowledgeAssistant.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;

var builder = WebApplication.CreateBuilder(args);

// 1. Layered Modular Service Registrations (AutoMapper, DB Context & Domain Services)
builder.Services.AddProviderServices(builder.Configuration);

// 2. Cookie Authentication for Tenant Administration & Local DataProtection
var keysFolder = Path.Combine(builder.Environment.ContentRootPath, "App_Data", "keys");
if (!Directory.Exists(keysFolder)) Directory.CreateDirectory(keysFolder);

builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(keysFolder))
    .SetApplicationName("AIKnowledgeAssistant");

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/Login";
        options.Cookie.Name = "AIKnowledgeAssistant.Auth";
        options.Cookie.HttpOnly = true;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });

// 3. CORS Policy for Embeddable Widget
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowEmbed", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });

var app = builder.Build();

// 4. Seed Initial Data (Apex University & FinTech Global)
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await SeedData.InitializeAsync(dbContext);
}

// 5. HTTP Pipeline Configuration
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseRouting();
app.UseCors("AllowEmbed");

app.UseAuthentication();
app.UseAuthorization();

// Dedicated Route for Public Visitor Assistant
app.MapControllerRoute(
    name: "assistant_view",
    pattern: "assistant/{token}",
    defaults: new { controller = "Assistant", action = "Index" });

// Default Route
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Dashboard}/{action=Index}/{id?}");

app.Run();
