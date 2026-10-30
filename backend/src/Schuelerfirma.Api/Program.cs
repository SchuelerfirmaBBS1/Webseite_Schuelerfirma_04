using Schuelerfirma.Api.Infrastructure.Http;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddProxyHeaderHandling();

var app = builder.Build();

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
