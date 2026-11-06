using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.DependencyInjection;
using Schuelerfirma.Api.Application.Auth;
using Schuelerfirma.Api.Infrastructure.Auth;
using Schuelerfirma.Api.Infrastructure.Http;
using Schuelerfirma.Api.Tests.Integration;

namespace Schuelerfirma.Api.Tests.Http;

public class ApiAreaRulesTests
{
    private static RouteEndpoint Endpoint(string route, string method, params object[] metadata) =>
        new(_ => Task.CompletedTask, RoutePatternFactory.Parse(route), 0,
            new EndpointMetadataCollection([new HttpMethodMetadata([method]), .. metadata]), route);

    private static readonly AllowAnonymousAttribute Anonymous = new();
    private static readonly HasPermissionAttribute EventsCreate = new(Permissions.EventsCreate);
    private static readonly EnableRateLimitingAttribute Limited = new(RateLimitPolicies.Contact);

    // ---- every rule finds its violation ----

    [Fact]
    public void APublicEndpointWithoutAllowAnonymous_IsAViolation()
    {
        var violation = Assert.Single(ApiAreaRules.Check([Endpoint("api/public/events", "GET")]));

        Assert.Contains("[AllowAnonymous]", violation);
    }

    [Fact]
    public void AnAdminEndpointWithAllowAnonymous_IsAViolation_EvenWithHasPermission()
    {
        var violation = Assert.Single(ApiAreaRules.Check([Endpoint("api/admin/events", "POST", EventsCreate, Anonymous)]));

        Assert.Contains("must not be [AllowAnonymous]", violation);
    }

    [Fact]
    public void AnAdminEndpointWithoutHasPermission_IsAViolation()
    {
        var violation = Assert.Single(ApiAreaRules.Check([Endpoint("api/admin/events", "GET")]));

        Assert.Contains("[HasPermission]", violation);
    }

    [Theory]
    [InlineData("POST")]
    [InlineData("PUT")]
    [InlineData("DELETE")]
    public void AWritingPublicEndpointWithoutRateLimit_IsAViolation(string method)
    {
        var violation = Assert.Single(ApiAreaRules.Check([Endpoint("api/public/contact-requests", method, Anonymous)]));

        Assert.Contains("[EnableRateLimiting]", violation);
    }

    [Theory]
    [InlineData("api/events")]
    [InlineData("api/publicity")] // only starts with the letters of "public"
    [InlineData("health")]
    public void AnEndpointOutsideTheThreeAreas_IsAViolation(string route)
    {
        var violation = Assert.Single(ApiAreaRules.Check([Endpoint(route, "GET", Anonymous)]));

        Assert.Contains("belong under", violation);
    }

    // ---- correct endpoints pass ----

    [Fact]
    public void EndpointsThatFollowTheRules_PassWithoutViolation()
    {
        Assert.Empty(ApiAreaRules.Check(
        [
            Endpoint("api/public/events", "GET", Anonymous),
            Endpoint("api/public/contact-requests", "POST", Anonymous, Limited),
            Endpoint("api/admin/events", "POST", EventsCreate),
            Endpoint("api/auth/login", "POST", Anonymous),
            Endpoint("api/auth/me", "GET"),
        ]));
    }

    // ---- the real API ----

    [Fact]
    public void AllEndpointsOfTheApi_FollowTheRules()
    {
        using var factory = new ApiFactory();
        factory.CreateHttpsClient();

        // Only the API's own endpoints; test controllers of this project are not part of the product.
        var endpoints = factory.Services.GetRequiredService<EndpointDataSource>().Endpoints
            .Where(e => e.Metadata.GetMetadata<ControllerActionDescriptor>() is not { } action
                || action.ControllerTypeInfo.Assembly == typeof(Program).Assembly)
            .ToList();

        Assert.NotEmpty(endpoints);
        Assert.Empty(ApiAreaRules.Check(endpoints));
    }
}
