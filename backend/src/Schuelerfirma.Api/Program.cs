using Schuelerfirma.Api.Application.Products;
using Schuelerfirma.Api.Application.Users;
using Schuelerfirma.Api.Infrastructure;
using Schuelerfirma.Api.Infrastructure.Auth;
using Schuelerfirma.Api.Infrastructure.Http;
using Schuelerfirma.Api.Infrastructure.InMemory;
using Schuelerfirma.Api.Infrastructure.OpenApi;
using Schuelerfirma.Api.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddProxyHeaderHandling();
builder.Services.AddPasswordSecurity();
builder.Services.AddSessionAuthentication();
builder.Services.AddRolePermissions();
builder.Services.AddScoped<UserAdministrationService>();
builder.Services.AddScoped<ProductCatalogService>();
builder.Services.AddCsrfProtection();
builder.Services.AddClientRateLimiting();

// Use MySQL when a connection string is configured; otherwise fall back to In-Memory in Development
// (outside Development the startup check below stops the application if no database is configured).
var connectionString = builder.Configuration.GetConnectionString("Default");
if (!string.IsNullOrEmpty(connectionString))
{
    builder.Services.AddMySqlPersistence(connectionString);
}
else if (builder.Environment.IsDevelopment())
{
    builder.Services.AddInMemoryPersistence();
}

// The API test UI (OpenAPI + Swagger UI, #51) exists only in Development, never on the public server.
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddApiDocumentation();
}

var app = builder.Build();

app.Services.EnsurePersistenceRegistered();

app.UseSecurityHeaders();
app.UseForwardedHeaders();

// In Development the built-in developer exception page is active; everywhere else
// errors are answered as ProblemDetails without stack traces.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler();
    app.UseHsts();
}

app.UseStatusCodePages();
app.UseHttpsRedirection();

// Before authentication: the fallback policy also covers requests without an endpoint, so the UI's static files
// would otherwise require a login.
if (app.Environment.IsDevelopment())
{
    app.UseApiTestUi();
}

// After UseForwardedHeaders (real client IP) and before the login middleware, so rejected requests cost nothing.
app.UseRateLimiter();
app.UseMiddleware<LoginStartsNewSessionMiddleware>();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

if (app.Environment.IsDevelopment())
{
    app.MapApiDocumentation();
}

app.Run();

/// <summary>Entry point marker so integration tests can use <c>WebApplicationFactory&lt;Program&gt;</c>.</summary>
public partial class Program;
