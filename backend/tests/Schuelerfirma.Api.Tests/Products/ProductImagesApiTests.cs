using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Schuelerfirma.Api.Application.Images;
using Schuelerfirma.Api.Tests.Auth;
using Schuelerfirma.Api.Tests.Images;
using Schuelerfirma.Api.Tests.Integration;

namespace Schuelerfirma.Api.Tests.Products;

public class ProductImagesApiTests : IDisposable
{
    private readonly AuthApiFactory factory = new();

    public void Dispose() => factory.Dispose();

    private static async Task<HttpClient> LoginAsync(WebApplicationFactory<Program> host, string username, string password)
    {
        var client = host.CreateHttpsClient();
        Assert.Equal(HttpStatusCode.NoContent, (await AuthApiFactory.LoginAsync(client, username, password)).StatusCode);
        return client;
    }

    private Task<HttpClient> AdminAsync() => LoginAsync(factory, AuthApiFactory.AdminUsername, AuthApiFactory.AdminPassword);

    private async Task<HttpClient> MemberAsync()
    {
        await factory.AddUserAsync("member");
        return await LoginAsync(factory, "member", AuthApiFactory.MemberPassword);
    }

    private static async Task<JsonElement> JsonAsync(HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<JsonElement>();

    private static async Task<int> CreateProductAsync(HttpClient admin, bool isActive = true, bool categoryVisible = true, string name = "Apfelsaft")
    {
        var category = await admin.PostAsJsonAsync("/api/admin/categories", new { name = "Kategorie " + name, isVisible = categoryVisible });
        Assert.Equal(HttpStatusCode.Created, category.StatusCode);
        var categoryId = (await JsonAsync(category)).GetProperty("categoryId").GetInt32();

        var product = await admin.PostAsJsonAsync(
            "/api/admin/products", new { categoryId, name, description = "Naturtrüb", price = 1.5m, isActive });
        Assert.Equal(HttpStatusCode.Created, product.StatusCode);
        return (await JsonAsync(product)).GetProperty("productId").GetInt32();
    }

    private static MultipartFormDataContent Files(params (string Name, byte[] Data, string ContentType)[] files)
    {
        var content = new MultipartFormDataContent();
        foreach (var (name, data, contentType) in files)
        {
            var part = new ByteArrayContent(data);
            part.Headers.ContentType = new MediaTypeHeaderValue(contentType);
            content.Add(part, "files", name);
        }

        return content;
    }

    private static Task<HttpResponseMessage> UploadAsync(HttpClient client, int productId, params (string Name, byte[] Data, string ContentType)[] files) =>
        client.PostAsync($"/api/admin/products/{productId}/images", Files(files));

    private static async Task<JsonElement> UploadOkAsync(HttpClient admin, int productId, params (string Name, byte[] Data, string ContentType)[] files)
    {
        var response = await UploadAsync(admin, productId, files);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return await JsonAsync(response);
    }

    private static (string, byte[], string) Jpeg(string name = "foto.jpg") => (name, TestImages.Jpeg(), "image/jpeg");

    private static string PublicUrl(int productId, int imageId) => $"/api/public/products/{productId}/images/{imageId}/file";

    private static string AdminUrl(int productId, int imageId) => $"/api/admin/products/{productId}/images/{imageId}/file";

    // ---- acceptance criteria ----

    [Fact]
    public async Task AnonymousVisitors_GetTheImagesOfVisibleProducts_ButCannotUpload()
    {
        var admin = await AdminAsync();
        var productId = await CreateProductAsync(admin);
        var imageId = (await UploadOkAsync(admin, productId, Jpeg()))[0].GetProperty("imageId").GetInt32();
        var anonymous = factory.CreateHttpsClient();

        Assert.Equal(HttpStatusCode.OK, (await anonymous.GetAsync(PublicUrl(productId, imageId))).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await UploadAsync(anonymous, productId, Jpeg())).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync(AdminUrl(productId, imageId))).StatusCode);
    }

    [Fact]
    public async Task RoleUser_MayViewImagesInTheAdminArea_ButNotUploadChangeOrDelete()
    {
        var admin = await AdminAsync();
        var productId = await CreateProductAsync(admin);
        var imageId = (await UploadOkAsync(admin, productId, Jpeg()))[0].GetProperty("imageId").GetInt32();
        var member = await MemberAsync();

        Assert.Equal(HttpStatusCode.OK, (await member.GetAsync(AdminUrl(productId, imageId))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await UploadAsync(member, productId, Jpeg())).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await member.PutAsJsonAsync(
            $"/api/admin/products/{productId}/images/{imageId}", new { description = "x", sortOrder = 0, isPrimary = true })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await member.DeleteAsync($"/api/admin/products/{productId}/images/{imageId}")).StatusCode);
    }

    [Fact]
    public async Task Upload_StoresAnImageWithoutGpsData_AndVisitorsGetItWithLongCaching()
    {
        var admin = await AdminAsync();
        var productId = await CreateProductAsync(admin);
        var withGps = TestImages.WithExif(TestImages.Jpeg());

        var created = await UploadOkAsync(admin, productId, ("urlaub.jpg", withGps, "image/jpeg"));
        var image = created.EnumerateArray().Single();
        var imageId = image.GetProperty("imageId").GetInt32();
        var file = await factory.CreateHttpsClient().GetAsync(PublicUrl(productId, imageId));
        var bytes = await file.Content.ReadAsByteArrayAsync();

        Assert.Equal("urlaub.jpg", image.GetProperty("fileName").GetString());
        Assert.Equal("image/jpeg", image.GetProperty("contentType").GetString());
        Assert.Equal(bytes.Length, image.GetProperty("length").GetInt64());
        Assert.True(image.GetProperty("isPrimary").GetBoolean());
        Assert.Equal(AdminUrl(productId, imageId), image.GetProperty("url").GetString());

        Assert.Equal(HttpStatusCode.OK, file.StatusCode);
        Assert.Equal("image/jpeg", file.Content.Headers.ContentType?.MediaType);
        Assert.Equal("public, max-age=31536000, immutable", file.Headers.CacheControl?.ToString());
        Assert.Equal("nosniff", file.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.False(TestImages.Contains(bytes, TestImages.SecretMarker));
        Assert.False(TestImages.Contains(bytes, "Exif"));
    }

    [Fact]
    public async Task PublicProduct_ListsItsImages_WithThePublicUrl_AndWithoutInternalFields()
    {
        var admin = await AdminAsync();
        var productId = await CreateProductAsync(admin);
        var imageId = (await UploadOkAsync(admin, productId, Jpeg()))[0].GetProperty("imageId").GetInt32();

        var product = await factory.CreateHttpsClient().GetFromJsonAsync<JsonElement>($"/api/public/products/{productId}");
        var image = product.GetProperty("images").EnumerateArray().Single();

        Assert.Equal(PublicUrl(productId, imageId), image.GetProperty("url").GetString());
        Assert.Equal(["url", "description", "isPrimary"], image.EnumerateObject().Select(p => p.Name));
    }

    [Fact]
    public async Task SvgOrHtml_DisguisedAsJpeg_IsRejected_AndNothingIsStored()
    {
        var admin = await AdminAsync();
        var productId = await CreateProductAsync(admin);

        var svg = await UploadAsync(admin, productId, ("bild.jpg", TestImages.Svg(), "image/jpeg"));
        var html = await UploadAsync(admin, productId, ("bild.jpg", TestImages.Html(), "image/jpeg"));
        var mixed = await UploadAsync(admin, productId, Jpeg(), ("zweites.jpg", TestImages.Svg(), "image/jpeg"));

        Assert.Equal(HttpStatusCode.BadRequest, svg.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, html.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, mixed.StatusCode);
        var error = (await JsonAsync(mixed)).GetProperty("errors").GetProperty("files")[0].GetString();
        Assert.Contains("zweites.jpg", error);
        var stored = await admin.GetFromJsonAsync<JsonElement>($"/api/admin/products/{productId}");
        Assert.Equal(0, stored.GetProperty("images").GetArrayLength());
    }

    [Fact]
    public async Task ImagesOfHiddenProducts_Are404ForVisitors_ButVisibleInTheAdminArea()
    {
        var admin = await AdminAsync();
        var inactive = await CreateProductAsync(admin, isActive: false, name: "Inaktiv");
        var inHidden = await CreateProductAsync(admin, categoryVisible: false, name: "Versteckt");
        var inactiveImage = (await UploadOkAsync(admin, inactive, Jpeg()))[0].GetProperty("imageId").GetInt32();
        var hiddenImage = (await UploadOkAsync(admin, inHidden, Jpeg()))[0].GetProperty("imageId").GetInt32();
        var anonymous = factory.CreateHttpsClient();

        var inactiveFile = await anonymous.GetAsync(PublicUrl(inactive, inactiveImage));
        Assert.Equal(HttpStatusCode.NotFound, inactiveFile.StatusCode);
        Assert.NotEqual(true, inactiveFile.Headers.CacheControl?.Public);
        Assert.Equal(HttpStatusCode.NotFound, (await anonymous.GetAsync(PublicUrl(inHidden, hiddenImage))).StatusCode);

        var adminFile = await admin.GetAsync(AdminUrl(inactive, inactiveImage));
        Assert.Equal(HttpStatusCode.OK, adminFile.StatusCode);
        Assert.True(adminFile.Headers.CacheControl?.NoStore);
    }

    [Fact]
    public async Task AnImage_UnderAnotherProduct_OrUnknown_Is404()
    {
        var admin = await AdminAsync();
        var hidden = await CreateProductAsync(admin, isActive: false, name: "Versteckt");
        var visible = await CreateProductAsync(admin, name: "Sichtbar");
        var hiddenImage = (await UploadOkAsync(admin, hidden, Jpeg()))[0].GetProperty("imageId").GetInt32();
        var anonymous = factory.CreateHttpsClient();

        // The image of the hidden product must not be reachable through the id of a visible one.
        Assert.Equal(HttpStatusCode.NotFound, (await anonymous.GetAsync(PublicUrl(visible, hiddenImage))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await anonymous.GetAsync(PublicUrl(visible, 9999))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.GetAsync(AdminUrl(visible, hiddenImage))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await UploadAsync(admin, 9999, Jpeg())).StatusCode);
    }

    // ---- upload rules ----

    [Fact]
    public async Task SeveralFiles_AreAppended_AndOnlyTheFirstImageOfAProductBecomesTheMainImage()
    {
        var admin = await AdminAsync();
        var productId = await CreateProductAsync(admin);

        var first = await UploadOkAsync(admin, productId, Jpeg("a.jpg"), ("b.png", TestImages.Png(), "image/png"));
        var second = await UploadOkAsync(admin, productId, ("c.webp", TestImages.Webp(), "image/webp"));

        Assert.Equal([0, 1], first.EnumerateArray().Select(i => i.GetProperty("sortOrder").GetInt32()));
        Assert.Equal([true, false], first.EnumerateArray().Select(i => i.GetProperty("isPrimary").GetBoolean()));
        Assert.Equal("image/png", first[1].GetProperty("contentType").GetString());
        Assert.Equal(2, second[0].GetProperty("sortOrder").GetInt32());
        Assert.False(second[0].GetProperty("isPrimary").GetBoolean());
        Assert.Equal("image/webp", second[0].GetProperty("contentType").GetString());
    }

    [Fact]
    public async Task UploadWithoutFiles_IsInvalid()
    {
        var admin = await AdminAsync();
        var productId = await CreateProductAsync(admin);

        var response = await admin.PostAsync($"/api/admin/products/{productId}/images", new MultipartFormDataContent { { new StringContent("x"), "other" } });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.True((await JsonAsync(response)).GetProperty("errors").TryGetProperty("files", out _));
    }

    [Fact]
    public async Task TooLargeFiles_AndRequests_Get413()
    {
        using var limited = factory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
            services.PostConfigure<ImageUploadOptions>(options =>
            {
                options.MaxFileSizeBytes = 2_000;
                options.MaxRequestSizeBytes = 6_000;
            })));
        var admin = await LoginAsync(limited, AuthApiFactory.AdminUsername, AuthApiFactory.AdminPassword);
        var productId = await CreateProductAsync(admin);
        var big = new byte[2_500];

        var tooLargeFile = await UploadAsync(admin, productId, ("gross.jpg", big, "image/jpeg"));
        var tooLargeRequest = await UploadAsync(admin, productId, ("a.jpg", new byte[1_900], "image/jpeg"),
            ("b.jpg", new byte[1_900], "image/jpeg"), ("c.jpg", new byte[1_900], "image/jpeg"), ("d.jpg", new byte[1_900], "image/jpeg"));

        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, tooLargeFile.StatusCode);
        Assert.Contains("gross.jpg", (await JsonAsync(tooLargeFile)).GetProperty("detail").GetString());
        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, tooLargeRequest.StatusCode);
    }

    [Fact]
    public async Task Upload_IsLoggedWithTheActingUser()
    {
        var admin = await AdminAsync();
        var productId = await CreateProductAsync(admin);

        var imageId = (await UploadOkAsync(admin, productId, Jpeg("log.jpg")))[0].GetProperty("imageId").GetInt32();

        Assert.Contains(factory.Logs.Entries, e => e.Message.Contains($"uploaded image {imageId} (log.jpg") && e.Message.StartsWith("User "));
    }

    // ---- edit and delete ----

    [Fact]
    public async Task Update_ChangesTheFields_AndKeepsOneMainImage()
    {
        var admin = await AdminAsync();
        var productId = await CreateProductAsync(admin);
        var images = await UploadOkAsync(admin, productId, Jpeg("a.jpg"), Jpeg("b.jpg"));
        var secondId = images[1].GetProperty("imageId").GetInt32();

        var response = await admin.PutAsJsonAsync(
            $"/api/admin/products/{productId}/images/{secondId}", new { description = "  Flasche von vorne  ", sortOrder = 5, isPrimary = true });
        var product = await admin.GetFromJsonAsync<JsonElement>($"/api/admin/products/{productId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var changed = await JsonAsync(response);
        Assert.Equal("Flasche von vorne", changed.GetProperty("description").GetString());
        Assert.Equal(5, changed.GetProperty("sortOrder").GetInt32());
        Assert.Equal([false, true], product.GetProperty("images").EnumerateArray().Select(i => i.GetProperty("isPrimary").GetBoolean()));
    }

    [Fact]
    public async Task Update_WithInvalidInput_Is400_AndUnknownImageIs404()
    {
        var admin = await AdminAsync();
        var productId = await CreateProductAsync(admin);
        var imageId = (await UploadOkAsync(admin, productId, Jpeg()))[0].GetProperty("imageId").GetInt32();
        var url = $"/api/admin/products/{productId}/images/{imageId}";

        var tooLong = await admin.PutAsJsonAsync(url, new { description = new string('a', 501), sortOrder = 0, isPrimary = true });
        var negative = await admin.PutAsJsonAsync(url, new { description = "x", sortOrder = -1, isPrimary = true });
        var missing = await admin.PutAsJsonAsync(url, new { description = "x" });
        var unknown = await admin.PutAsJsonAsync($"/api/admin/products/{productId}/images/9999", new { description = "x", sortOrder = 0, isPrimary = false });

        Assert.Equal(HttpStatusCode.BadRequest, tooLong.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, negative.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, missing.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);
    }

    [Fact]
    public async Task Delete_RemovesTheImage_AndDeletingTheProductRemovesItsImages()
    {
        var admin = await AdminAsync();
        var productId = await CreateProductAsync(admin);
        var images = await UploadOkAsync(admin, productId, Jpeg("a.jpg"), Jpeg("b.jpg"));
        var firstId = images[0].GetProperty("imageId").GetInt32();
        var secondId = images[1].GetProperty("imageId").GetInt32();
        var anonymous = factory.CreateHttpsClient();

        Assert.Equal(HttpStatusCode.NoContent, (await admin.DeleteAsync($"/api/admin/products/{productId}/images/{firstId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.DeleteAsync($"/api/admin/products/{productId}/images/{firstId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await anonymous.GetAsync(PublicUrl(productId, firstId))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await anonymous.GetAsync(PublicUrl(productId, secondId))).StatusCode);

        Assert.Equal(HttpStatusCode.NoContent, (await admin.DeleteAsync($"/api/admin/products/{productId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.GetAsync(AdminUrl(productId, secondId))).StatusCode);
    }

    [Fact]
    public async Task Upload_WithoutCsrfToken_IsRejected()
    {
        var admin = await AdminAsync();
        var productId = await CreateProductAsync(admin);
        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/admin/products/{productId}/images") { Content = Files(Jpeg()) };
        request.Headers.Add("X-XSRF-TOKEN", "falsch");

        var response = await admin.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var stored = await admin.GetFromJsonAsync<JsonElement>($"/api/admin/products/{productId}");
        Assert.Equal(0, stored.GetProperty("images").GetArrayLength());
    }
}
