using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Schuelerfirma.Api.Tests.Auth;
using Schuelerfirma.Api.Tests.Integration;

namespace Schuelerfirma.Api.Tests.Products;

public class ProductsApiTests : IDisposable
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

    private static async Task<JsonElement> JsonAsync(HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<JsonElement>();

    private static async Task<int> CreateCategoryAsync(HttpClient admin, string name, bool isVisible = true)
    {
        var response = await admin.PostAsJsonAsync("/api/admin/categories", new { name, isVisible });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await JsonAsync(response)).GetProperty("categoryId").GetInt32();
    }

    private static async Task<int> CreateProductAsync(
        HttpClient admin, int categoryId, string name, decimal? price = 2.50m, bool isPriceVisible = true, bool isActive = true)
    {
        var response = await admin.PostAsJsonAsync(
            "/api/admin/products", new { categoryId, name, description = "Beschreibung", price, isPriceVisible, isActive });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await JsonAsync(response)).GetProperty("productId").GetInt32();
    }

    private HttpClient Anonymous() => factory.CreateHttpsClient();

    // ---- acceptance criteria ----

    [Fact]
    public async Task AnonymousVisitors_SeeThePublicList_ButGet401InTheAdminArea()
    {
        var anonymous = Anonymous();

        Assert.Equal(HttpStatusCode.OK, (await anonymous.GetAsync("/api/public/products")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await anonymous.GetAsync("/api/public/categories")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/api/admin/products")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/api/admin/categories")).StatusCode);
    }

    [Fact]
    public async Task InactiveProducts_AndProductsInHiddenCategories_AreMissing_AndTheirDetailIs404LikeAnUnknownId()
    {
        var admin = await AdminAsync();
        var drinks = await CreateCategoryAsync(admin, "Getränke");
        var hidden = await CreateCategoryAsync(admin, "Geheim", isVisible: false);
        var visible = await CreateProductAsync(admin, drinks, "Wasser");
        var inactive = await CreateProductAsync(admin, drinks, "Alter Saft", isActive: false);
        var inHidden = await CreateProductAsync(admin, hidden, "Kekse");
        var anonymous = Anonymous();

        var list = await anonymous.GetFromJsonAsync<JsonElement>("/api/public/products");
        var inactiveDetail = await anonymous.GetAsync($"/api/public/products/{inactive}");
        var hiddenDetail = await anonymous.GetAsync($"/api/public/products/{inHidden}");
        var unknownDetail = await anonymous.GetAsync("/api/public/products/4711");

        Assert.Equal([visible], list.EnumerateArray().Select(p => p.GetProperty("id").GetInt32()));
        Assert.Equal(HttpStatusCode.OK, (await anonymous.GetAsync($"/api/public/products/{visible}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, inactiveDetail.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, hiddenDetail.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, unknownDetail.StatusCode);
        var unknownTitle = (await JsonAsync(unknownDetail)).GetProperty("title").GetString();
        Assert.Equal(unknownTitle, (await JsonAsync(inactiveDetail)).GetProperty("title").GetString());
        Assert.Equal(unknownTitle, (await JsonAsync(hiddenDetail)).GetProperty("title").GetString());
    }

    [Fact]
    public async Task AHiddenPrice_IsNullInTheRawPublicJson_ButTheAdminAreaShowsTheRealPrice()
    {
        var admin = await AdminAsync();
        var drinks = await CreateCategoryAsync(admin, "Getränke");
        var id = await CreateProductAsync(admin, drinks, "Apfelsaft", price: 47.11m, isPriceVisible: false);
        var anonymous = Anonymous();

        var detail = await anonymous.GetStringAsync($"/api/public/products/{id}");
        var list = await anonymous.GetStringAsync("/api/public/products");
        var adminDetail = await admin.GetFromJsonAsync<JsonElement>($"/api/admin/products/{id}");

        Assert.Contains("\"price\":null", detail);
        Assert.Contains("\"price\":null", list);
        Assert.DoesNotContain("47.11", detail);
        Assert.DoesNotContain("47.11", list);
        Assert.Equal(47.11m, adminDetail.GetProperty("price").GetDecimal());
        Assert.False(adminDetail.GetProperty("isPriceVisible").GetBoolean());
    }

    [Fact]
    public async Task TheRoleUser_MayToggleThePriceVisibility_ButNotChangeTheProduct()
    {
        var admin = await AdminAsync();
        var drinks = await CreateCategoryAsync(admin, "Getränke");
        var id = await CreateProductAsync(admin, drinks, "Apfelsaft", price: 1.50m);
        var member = await MemberAsync();

        var toggle = await member.PutAsJsonAsync($"/api/admin/products/{id}/price-visibility", new { isPriceVisible = false });
        var update = await member.PutAsJsonAsync(
            $"/api/admin/products/{id}", new { categoryId = drinks, name = "Gehackt", price = 0, isPriceVisible = true, isActive = true });

        Assert.Equal(HttpStatusCode.NoContent, toggle.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, update.StatusCode);
        var publicDetail = await Anonymous().GetFromJsonAsync<JsonElement>($"/api/public/products/{id}");
        Assert.Equal(JsonValueKind.Null, publicDetail.GetProperty("price").ValueKind);
        Assert.Equal("Apfelsaft", publicDetail.GetProperty("name").GetString());
    }

    // ---- access ----

    [Theory]
    [InlineData("POST", "/api/admin/products")]
    [InlineData("DELETE", "/api/admin/products/1")]
    [InlineData("POST", "/api/admin/categories")]
    [InlineData("PUT", "/api/admin/categories/1")]
    [InlineData("DELETE", "/api/admin/categories/1")]
    public async Task TheRoleUser_GetsA403_ForManagingEndpoints_AndAnonymousClientsA401(string method, string path)
    {
        var member = await MemberAsync();

        var asMember = await member.SendAsync(new HttpRequestMessage(new HttpMethod(method), path) { Content = JsonContent.Create(new { }) });
        var anonymous = await Anonymous().SendAsync(new HttpRequestMessage(new HttpMethod(method), path) { Content = JsonContent.Create(new { }) });

        Assert.Equal(HttpStatusCode.Forbidden, asMember.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);
    }

    [Fact]
    public async Task TheRoleUser_MayReadTheAdminLists()
    {
        var member = await MemberAsync();

        Assert.Equal(HttpStatusCode.OK, (await member.GetAsync("/api/admin/products")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await member.GetAsync("/api/admin/categories")).StatusCode);
    }

    [Fact]
    public async Task AdminResponses_AreNotCached()
    {
        var response = await (await AdminAsync()).GetAsync("/api/admin/products");

        Assert.Contains("no-store", response.Headers.CacheControl?.ToString());
    }

    // ---- public responses ----

    [Fact]
    public async Task ThePublicProduct_HasNoInternalFields()
    {
        var admin = await AdminAsync();
        var drinks = await CreateCategoryAsync(admin, "Getränke");
        var id = await CreateProductAsync(admin, drinks, "Apfelsaft");

        var json = await Anonymous().GetFromJsonAsync<JsonElement>($"/api/public/products/{id}");

        Assert.Equal(
            ["id", "categoryId", "categoryName", "name", "description", "price", "images"],
            json.EnumerateObject().Select(p => p.Name));
        Assert.Equal("Getränke", json.GetProperty("categoryName").GetString());
        Assert.Equal(2.50m, json.GetProperty("price").GetDecimal());
        Assert.Equal(0, json.GetProperty("images").GetArrayLength());
    }

    [Fact]
    public async Task ThePublicList_IsOrderedByName_AndFiltersByCategory()
    {
        var admin = await AdminAsync();
        var drinks = await CreateCategoryAsync(admin, "Getränke");
        var snacks = await CreateCategoryAsync(admin, "Snacks");
        await CreateProductAsync(admin, drinks, "Wasser");
        await CreateProductAsync(admin, drinks, "Apfelsaft");
        await CreateProductAsync(admin, snacks, "Kekse");
        var anonymous = Anonymous();

        var all = await anonymous.GetFromJsonAsync<JsonElement>("/api/public/products");
        var onlyDrinks = await anonymous.GetFromJsonAsync<JsonElement>($"/api/public/products?categoryId={drinks}");

        Assert.Equal(["Apfelsaft", "Kekse", "Wasser"], all.EnumerateArray().Select(p => p.GetProperty("name").GetString()));
        Assert.Equal(["Apfelsaft", "Wasser"], onlyDrinks.EnumerateArray().Select(p => p.GetProperty("name").GetString()));
    }

    [Fact]
    public async Task ThePublicCategories_AreOnlyTheVisibleOnes_WithoutInternalFields()
    {
        var admin = await AdminAsync();
        await CreateCategoryAsync(admin, "Snacks");
        await CreateCategoryAsync(admin, "Geheim", isVisible: false);
        await CreateCategoryAsync(admin, "Getränke");

        var json = await Anonymous().GetFromJsonAsync<JsonElement>("/api/public/categories");

        Assert.Equal(["Getränke", "Snacks"], json.EnumerateArray().Select(c => c.GetProperty("name").GetString()));
        Assert.Equal(["id", "name"], json[0].EnumerateObject().Select(p => p.Name));
    }

    [Fact]
    public async Task Texts_AreReturnedAsTheyWereEntered_NotInterpretedAsHtml()
    {
        var admin = await AdminAsync();
        var drinks = await CreateCategoryAsync(admin, "Getränke");
        var id = await CreateProductAsync(admin, drinks, "<b>Saft</b>");

        var json = await Anonymous().GetFromJsonAsync<JsonElement>($"/api/public/products/{id}");

        Assert.Equal("<b>Saft</b>", json.GetProperty("name").GetString());
    }

    // ---- admin area ----

    [Fact]
    public async Task ANewProduct_IsActiveWithVisiblePrice_ByDefault()
    {
        var admin = await AdminAsync();
        var drinks = await CreateCategoryAsync(admin, "Getränke");

        var created = await admin.PostAsJsonAsync("/api/admin/products", new { categoryId = drinks, name = "Wasser", price = 1.00m });
        var body = await JsonAsync(created);

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.True(body.GetProperty("isActive").GetBoolean());
        Assert.True(body.GetProperty("isPriceVisible").GetBoolean());
        Assert.EndsWith($"/api/admin/products/{body.GetProperty("productId").GetInt32()}", created.Headers.Location?.ToString());
    }

    [Fact]
    public async Task TheAdminList_ContainsHiddenProducts_WithAllFlags()
    {
        var admin = await AdminAsync();
        var hidden = await CreateCategoryAsync(admin, "Geheim", isVisible: false);
        await CreateProductAsync(admin, hidden, "Kekse", isActive: false, isPriceVisible: false);

        var list = await admin.GetFromJsonAsync<JsonElement>("/api/admin/products");
        var product = list.EnumerateArray().Single();

        Assert.False(product.GetProperty("isActive").GetBoolean());
        Assert.False(product.GetProperty("isPriceVisible").GetBoolean());
        Assert.Equal(2.50m, product.GetProperty("price").GetDecimal());
    }

    [Theory]
    [InlineData(1.234)]
    [InlineData(-1)]
    public async Task AnInvalidPrice_Is400AtThePriceField(double price)
    {
        var admin = await AdminAsync();
        var drinks = await CreateCategoryAsync(admin, "Getränke");

        var response = await admin.PostAsJsonAsync("/api/admin/products", new { categoryId = drinks, name = "Wasser", price });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.True((await JsonAsync(response)).GetProperty("errors").TryGetProperty("Price", out _));
    }

    [Fact]
    public async Task MissingName_AndUnknownCategory_Are400()
    {
        var admin = await AdminAsync();
        var drinks = await CreateCategoryAsync(admin, "Getränke");

        var noName = await admin.PostAsJsonAsync("/api/admin/products", new { categoryId = drinks, price = 1 });
        var unknownCategory = await admin.PostAsJsonAsync("/api/admin/products", new { categoryId = 4711, name = "Wasser" });
        var tooLong = await admin.PostAsJsonAsync("/api/admin/products", new { categoryId = drinks, name = new string('a', 201) });

        Assert.Equal(HttpStatusCode.BadRequest, noName.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, unknownCategory.StatusCode);
        Assert.Equal("Diese Kategorie gibt es nicht.", (await JsonAsync(unknownCategory)).GetProperty("errors").GetProperty("CategoryId")[0].GetString());
        Assert.Equal(HttpStatusCode.BadRequest, tooLong.StatusCode);
    }

    [Fact]
    public async Task UpdateAndDelete_OfAProduct()
    {
        var admin = await AdminAsync();
        var drinks = await CreateCategoryAsync(admin, "Getränke");
        var id = await CreateProductAsync(admin, drinks, "Apfelsaft");

        var update = await admin.PutAsJsonAsync(
            $"/api/admin/products/{id}",
            new { categoryId = drinks, name = "Birnensaft", description = (string?)null, price = 3.00m, isPriceVisible = true, isActive = true });
        var delete = await admin.DeleteAsync($"/api/admin/products/{id}");

        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        Assert.Equal("Birnensaft", (await JsonAsync(update)).GetProperty("name").GetString());
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.GetAsync($"/api/admin/products/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.DeleteAsync($"/api/admin/products/{id}")).StatusCode);
        Assert.Equal(
            HttpStatusCode.NotFound,
            (await admin.PutAsJsonAsync($"/api/admin/products/{id}/price-visibility", new { isPriceVisible = true })).StatusCode);
    }

    [Fact]
    public async Task ADuplicateCategoryName_Is409()
    {
        var admin = await AdminAsync();
        await CreateCategoryAsync(admin, "Getränke");

        var response = await admin.PostAsJsonAsync("/api/admin/categories", new { name = "GETRÄNKE", isVisible = true });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("Es gibt schon eine Kategorie mit diesem Namen.", (await JsonAsync(response)).GetProperty("detail").GetString());
    }

    [Fact]
    public async Task DeletingACategoryWithProducts_Is409_AnEmptyOneIsDeleted()
    {
        var admin = await AdminAsync();
        var drinks = await CreateCategoryAsync(admin, "Getränke");
        var empty = await CreateCategoryAsync(admin, "Leer");
        await CreateProductAsync(admin, drinks, "Wasser");

        Assert.Equal(HttpStatusCode.Conflict, (await admin.DeleteAsync($"/api/admin/categories/{drinks}")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await admin.DeleteAsync($"/api/admin/categories/{empty}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.DeleteAsync($"/api/admin/categories/{empty}")).StatusCode);
    }

    [Fact]
    public async Task HidingACategory_HidesItsProductsForVisitors()
    {
        var admin = await AdminAsync();
        var drinks = await CreateCategoryAsync(admin, "Getränke");
        var id = await CreateProductAsync(admin, drinks, "Wasser");

        var update = await admin.PutAsJsonAsync($"/api/admin/categories/{drinks}", new { name = "Getränke", isVisible = false });

        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Anonymous().GetAsync($"/api/public/products/{id}")).StatusCode);
        Assert.Equal(0, (await Anonymous().GetFromJsonAsync<JsonElement>("/api/public/categories")).GetArrayLength());
    }
}
