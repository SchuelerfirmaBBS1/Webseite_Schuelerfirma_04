using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Schuelerfirma.Api.Application.Auth;
using Schuelerfirma.Api.Controllers;
using Schuelerfirma.Api.Domain;
using Schuelerfirma.Api.Infrastructure.Auth;
using Schuelerfirma.Api.Tests.Integration;

namespace Schuelerfirma.Api.Tests.Auth;

/// <summary>Admin-area endpoints that only differ in the permission they require.</summary>
[Route("api/admin/permission-probe")]
public class PermissionProbeController : AdminApiController
{
    [HttpGet("users")]
    [HasPermission(Permissions.UsersManage)]
    public IActionResult Users() => Ok();

    [HttpGet("events")]
    [HasPermission(Permissions.EventsCreate)]
    public IActionResult Events() => Ok();

    /// <summary>Two attributes: both permissions are required.</summary>
    [HttpGet("create-and-delete")]
    [HasPermission(Permissions.EventsCreate)]
    [HasPermission(Permissions.EventsDelete)]
    public IActionResult CreateAndDelete() => Ok();
}

/// <summary>A public endpoint that only relies on the base class.</summary>
public class PublicProbeController : PublicApiController
{
    [HttpGet]
    public IActionResult Get() => Ok();
}

public class PermissionTests : IDisposable
{
    private readonly AuthApiFactory factory = new();

    public void Dispose() => factory.Dispose();

    private async Task<HttpClient> LoginAsync(string username, string password)
    {
        var client = factory.CreateHttpsClient();
        Assert.Equal(HttpStatusCode.NoContent, (await AuthApiFactory.LoginAsync(client, username, password)).StatusCode);
        return client;
    }

    private Task<HttpClient> AdminAsync() => LoginAsync(AuthApiFactory.AdminUsername, AuthApiFactory.AdminPassword);

    private async Task<HttpClient> MemberAsync()
    {
        await factory.AddUserAsync("member");
        return await LoginAsync("member", AuthApiFactory.MemberPassword);
    }

    private static async Task<HttpStatusCode> StatusAsync(HttpClient client, string path) => (await client.GetAsync(path)).StatusCode;

    [Fact]
    public async Task TheRoleUser_MayCreateEvents_ButNotManageUsers()
    {
        var member = await MemberAsync();

        Assert.Equal(HttpStatusCode.OK, await StatusAsync(member, "/api/admin/permission-probe/events"));
        Assert.Equal(HttpStatusCode.Forbidden, await StatusAsync(member, "/api/admin/permission-probe/users"));
    }

    [Fact]
    public async Task TheRoleAdmin_MayDoBoth()
    {
        var admin = await AdminAsync();

        Assert.Equal(HttpStatusCode.OK, await StatusAsync(admin, "/api/admin/permission-probe/events"));
        Assert.Equal(HttpStatusCode.OK, await StatusAsync(admin, "/api/admin/permission-probe/users"));
    }

    [Fact]
    public async Task WithoutLogin_TheAnswerIs401_Not403()
    {
        Assert.Equal(HttpStatusCode.Unauthorized, await StatusAsync(factory.CreateHttpsClient(), "/api/admin/permission-probe/events"));
    }

    [Fact]
    public async Task AMissingPermission_IsAnsweredWithAProblemDetails()
    {
        var response = await (await MemberAsync()).GetAsync("/api/admin/permission-probe/users");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Null(response.Headers.Location);
    }

    [Fact]
    public async Task SeveralAttributes_MustAllBeSatisfied()
    {
        // The role User has events.create but not events.delete.
        Assert.Equal(HttpStatusCode.Forbidden, await StatusAsync(await MemberAsync(), "/api/admin/permission-probe/create-and-delete"));
        Assert.Equal(HttpStatusCode.OK, await StatusAsync(await AdminAsync(), "/api/admin/permission-probe/create-and-delete"));
    }

    [Fact]
    public async Task ARoleChange_AppliesToPermissionsImmediately()
    {
        var member = await MemberAsync();
        Assert.Equal(HttpStatusCode.Forbidden, await StatusAsync(member, "/api/admin/permission-probe/users"));

        var user = (await factory.Users.GetByUsernameAsync("member"))!;
        user.RoleId = 1;
        await factory.Users.UpdateAsync(user);

        Assert.Equal(HttpStatusCode.OK, await StatusAsync(member, "/api/admin/permission-probe/users"));
    }

    [Fact]
    public async Task AdminAreaResponses_AreNotCacheable()
    {
        var response = await (await AdminAsync()).GetAsync("/api/admin/permission-probe/events");

        Assert.Contains("no-store", response.Headers.CacheControl?.ToString());
    }

    [Fact]
    public async Task ThePublicBaseClass_MakesEndpointsReachableWithoutLogin_UnderApiPublic()
    {
        Assert.Equal(HttpStatusCode.OK, await StatusAsync(factory.CreateHttpsClient(), "/api/public/publicprobe"));
    }

    // ---- /me ----

    [Fact]
    public async Task Me_ListsThePermissionsOfTheRole_WithTheWildcardExpanded()
    {
        var admin = await (await AdminAsync()).GetFromJsonAsync<JsonElement>("/api/auth/me");
        var member = await (await MemberAsync()).GetFromJsonAsync<JsonElement>("/api/auth/me");

        Assert.Equal(Permissions.All, admin.GetProperty("permissions").EnumerateArray().Select(p => p.GetString()));
        Assert.Equal(
            [
                Permissions.EventsCreate,
                Permissions.EventsUpdate,
                Permissions.ImagesUpload,
                Permissions.ProductsRead,
                Permissions.ProductsTogglePriceVisibility,
            ],
            member.GetProperty("permissions").EnumerateArray().Select(p => p.GetString()));
        Assert.Equal(DefaultRoles.User, member.GetProperty("role").GetString());
    }

    // ---- startup ----

    [Fact]
    public void ATypoInThePermissions_PreventsTheStart()
    {
        using var invalid = factory.WithWebHostBuilder(builder => builder.ConfigureAppConfiguration(
            (_, configuration) => configuration.AddInMemoryCollection(
                new Dictionary<string, string?> { ["Authorization:RolePermissions:User:0"] = "events.creat" })));

        var exception = Assert.ThrowsAny<Exception>(() => invalid.CreateHttpsClient());

        Assert.Contains("events.creat", exception.ToString());
    }

    [Fact]
    public void ARoleThatDoesNotExist_IsReportedAsAWarning()
    {
        using var unknownRole = factory.WithWebHostBuilder(builder => builder.ConfigureAppConfiguration(
            (_, configuration) => configuration.AddInMemoryCollection(
                new Dictionary<string, string?> { ["Authorization:RolePermissions:Gast:0"] = Permissions.EventsCreate })));

        unknownRole.CreateHttpsClient();

        Assert.Contains(factory.Logs.Entries, e => e.Level == LogLevel.Warning && e.Message.Contains("'Gast'"));
        Assert.DoesNotContain(factory.Logs.Entries, e => e.Message.Contains("'Admin', which does not exist"));
    }
}
