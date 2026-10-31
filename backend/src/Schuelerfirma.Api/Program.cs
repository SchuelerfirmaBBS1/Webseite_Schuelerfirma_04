using Schuelerfirma.Api.Infrastructure;
using Schuelerfirma.Api.Infrastructure.Auth;
using Schuelerfirma.Api.Infrastructure.Http;
using Schuelerfirma.Api.Infrastructure.InMemory;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddProxyHeaderHandling();
builder.Services.AddPasswordSecurity();

// Until the EF Core repositories exist (#22) there is no real storage; outside Development the
// startup check below stops the application instead of running without a database.
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddInMemoryPersistence();
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

app.MapControllers();

app.Run();

/// <summary>Entry point marker so integration tests can use <c>WebApplicationFactory&lt;Program&gt;</c>.</summary>
public partial class Program;
