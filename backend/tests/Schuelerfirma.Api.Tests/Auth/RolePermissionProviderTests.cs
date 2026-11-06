using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Schuelerfirma.Api.Application.Auth;
using Schuelerfirma.Api.Infrastructure.Auth;
using Schuelerfirma.Api.Tests.Integration;

namespace Schuelerfirma.Api.Tests.Auth;

public class RolePermissionProviderTests
{
    /// <summary>An options monitor whose value the test can change, like a reloaded appsettings.json.</summary>
    private sealed class ChangeableOptions(RolePermissionOptions value) : IOptionsMonitor<RolePermissionOptions>
    {
        private Action<RolePermissionOptions, string?>? listener;

        public RolePermissionOptions CurrentValue { get; private set; } = value;

        public RolePermissionOptions Get(string? name) => CurrentValue;

        public IDisposable OnChange(Action<RolePermissionOptions, string?> callback)
        {
            listener = callback;
            return new Subscription();
        }

        public void Change(RolePermissionOptions value)
        {
            CurrentValue = value;
            listener?.Invoke(value, Options.DefaultName);
        }

        private sealed class Subscription : IDisposable
        {
            public void Dispose()
            {
            }
        }
    }

    private static RolePermissionOptions Config(params (string Role, string[] Permissions)[] roles)
    {
        var options = new RolePermissionOptions();
        foreach (var (role, permissions) in roles)
        {
            options.RolePermissions[role] = permissions;
        }

        return options;
    }

    private static ConfiguredRolePermissionProvider Provider(RolePermissionOptions options, ListLoggerProvider? logs = null)
    {
        var loggerFactory = LoggerFactory.Create(builder =>
        {
            if (logs is not null)
            {
                builder.AddProvider(logs);
            }
        });
        return new ConfiguredRolePermissionProvider(new ChangeableOptions(options), loggerFactory.CreateLogger<ConfiguredRolePermissionProvider>());
    }

    // ---- catalog ----

    [Fact]
    public void TheCatalog_ContainsTheAgreedPermissions_SortedAndUnique()
    {
        Assert.Equal(14, Permissions.All.Count);
        Assert.Equal(Permissions.All.Order(StringComparer.Ordinal), Permissions.All);
        Assert.Equal(Permissions.All.Count, Permissions.All.Distinct().Count());
        Assert.Contains(Permissions.UsersManage, Permissions.All);
        Assert.DoesNotContain(Permissions.Wildcard, Permissions.All);
    }

    [Fact]
    public void HasPermission_WithANameOutsideTheCatalog_FailsImmediately()
    {
        Assert.Throws<ArgumentException>(() => new HasPermissionAttribute("events.creat"));
        Assert.Throws<ArgumentException>(() => new HasPermissionAttribute(Permissions.Wildcard));
    }

    // ---- lookups ----

    [Fact]
    public void TheWildcard_GrantsEveryPermissionOfTheCatalog()
    {
        var provider = Provider(Config(("Admin", [Permissions.Wildcard])));

        Assert.Equal(Permissions.All.ToHashSet(), provider.GetPermissions("Admin").ToHashSet());
        Assert.All(Permissions.All, permission => Assert.True(provider.HasPermission("Admin", permission)));
    }

    [Fact]
    public void ARole_HasExactlyTheListedPermissions()
    {
        var provider = Provider(Config(("User", [Permissions.EventsCreate, Permissions.ImagesUpload])));

        Assert.True(provider.HasPermission("User", Permissions.EventsCreate));
        Assert.True(provider.HasPermission("User", Permissions.ImagesUpload));
        Assert.False(provider.HasPermission("User", Permissions.EventsDelete));
        Assert.False(provider.HasPermission("User", Permissions.UsersManage));
    }

    [Fact]
    public void RoleNames_AreCaseInsensitive_PermissionNamesAreNot()
    {
        var provider = Provider(Config(("User", [Permissions.EventsCreate])));

        Assert.True(provider.HasPermission("user", Permissions.EventsCreate));
        Assert.True(provider.HasPermission("USER", Permissions.EventsCreate));
        Assert.False(provider.HasPermission("User", "EVENTS.CREATE"));
    }

    [Fact]
    public void ARoleWithoutEntry_OrWithAnEmptyList_HasNoPermissions()
    {
        var provider = Provider(Config(("Empty", [])));

        Assert.Empty(provider.GetPermissions("Empty"));
        Assert.Empty(provider.GetPermissions("Unknown"));
        Assert.False(provider.HasPermission("Unknown", Permissions.EventsCreate));
    }

    // ---- invalid configuration ----

    [Fact]
    public void ATypoInTheConfiguration_IsRejected_WithTheWrongName()
    {
        var exception = Assert.Throws<InvalidOperationException>(() => Provider(Config(("User", ["events.creat"]))));

        Assert.Contains("events.creat", exception.Message);
        Assert.Contains("User", exception.Message);
    }

    [Fact]
    public void AValidChange_AppliesWithoutRestart()
    {
        var options = new ChangeableOptions(Config(("User", [Permissions.EventsCreate])));
        var provider = new ConfiguredRolePermissionProvider(options, LoggerFactory.Create(_ => { }).CreateLogger<ConfiguredRolePermissionProvider>());

        options.Change(Config(("User", [Permissions.EventsDelete])));

        Assert.False(provider.HasPermission("User", Permissions.EventsCreate));
        Assert.True(provider.HasPermission("User", Permissions.EventsDelete));
    }

    [Fact]
    public void AnInvalidChange_IsIgnored_AndLogged_TheLastValidAssignmentStays()
    {
        var logs = new ListLoggerProvider();
        var options = new ChangeableOptions(Config(("User", [Permissions.EventsCreate])));
        var provider = new ConfiguredRolePermissionProvider(
            options, LoggerFactory.Create(builder => builder.AddProvider(logs)).CreateLogger<ConfiguredRolePermissionProvider>());

        options.Change(Config(("User", ["events.delet"])));

        Assert.True(provider.HasPermission("User", Permissions.EventsCreate));
        Assert.Contains(logs.Entries, e => e.Level == LogLevel.Error && e.Message.Contains("events.delet"));
    }

    // ---- shipped configuration ----

    [Fact]
    public void TheShippedConfiguration_FollowsTheTeachersRules()
    {
        using var factory = new ApiFactory();
        factory.CreateHttpsClient();
        var provider = factory.Services.GetRequiredService<IRolePermissionProvider>();

        Assert.Equal(Permissions.All.ToHashSet(), provider.GetPermissions("Admin").ToHashSet());
        Assert.Equal(
            new HashSet<string>
            {
                Permissions.EventsCreate,
                Permissions.EventsUpdate,
                Permissions.ImagesUpload,
                Permissions.ProductsRead,
                Permissions.ProductsTogglePriceVisibility,
            },
            provider.GetPermissions("User").ToHashSet());
    }
}
