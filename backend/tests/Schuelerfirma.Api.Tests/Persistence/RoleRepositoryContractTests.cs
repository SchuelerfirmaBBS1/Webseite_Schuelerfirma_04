using Schuelerfirma.Api.Domain;

namespace Schuelerfirma.Api.Tests.Persistence;

public abstract class RoleRepositoryContractTests : RepositoryContractTests
{
    [Fact]
    public async Task GetAll_ReturnsSeedRolesOrderedById()
    {
        var roles = await CreateRepositories().Roles.GetAllAsync();

        Assert.Equal([DefaultRoles.Admin, DefaultRoles.User], roles.Select(r => r.Name));
        Assert.Equal(roles.Select(r => r.RoleId).Order(), roles.Select(r => r.RoleId));
    }

    [Fact]
    public async Task GetById_ReturnsRole_OrNullWhenUnknown()
    {
        var repos = CreateRepositories();

        var admin = await repos.Roles.GetByIdAsync(AdminRoleId);

        Assert.Equal(DefaultRoles.Admin, admin?.Name);
        Assert.Null(await repos.Roles.GetByIdAsync(9999));
    }

    [Fact]
    public async Task GetByName_IgnoresCase_AndReturnsNullWhenUnknown()
    {
        var repos = CreateRepositories();

        var role = await repos.Roles.GetByNameAsync("aDmIn");

        Assert.Equal(AdminRoleId, role?.RoleId);
        Assert.Null(await repos.Roles.GetByNameAsync("Superuser"));
    }
}
