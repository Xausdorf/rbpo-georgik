using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using GetFast.Api.Auth;
using GetFast.Api.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace GetFast.Api.Tests;

public sealed class SwaggerTests(PostgreSqlFixture database) : IClassFixture<PostgreSqlFixture>
{
    [Theory]
    [InlineData("Development", "/swagger/index.html", HttpStatusCode.OK)]
    [InlineData("Development", "/swagger/v1/swagger.json", HttpStatusCode.OK)]
    [InlineData("Production", "/swagger/index.html", HttpStatusCode.Unauthorized)]
    [InlineData("Production", "/swagger/v1/swagger.json", HttpStatusCode.Unauthorized)]
    public async Task Swagger_IsAvailableOnlyInDevelopment(
        string environment, string path, HttpStatusCode expectedStatus)
    {
        await using var factory = new GetFastApiFactory(database.ConnectionString, environment);
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(path);

        Assert.Equal(expectedStatus, response.StatusCode);
    }

    [Theory]
    [InlineData("/swagger/index.html")]
    [InlineData("/swagger/v1/swagger.json")]
    public async Task Production_SwaggerDoesNotExist_EvenForAuthenticatedUser(string path)
    {
        await using var factory = new GetFastApiFactory(database.ConnectionString, "Production");
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
            factory.Services.GetRequiredService<JwtTokenService>().Create(new AppUser { Id = Guid.NewGuid() }, [RoleNames.Dispatcher]).AccessToken);
        using var response = await client.GetAsync(path);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Development_OpenApi_DescribesBearerAndAnonymousExceptions()
    {
        await using var factory = new GetFastApiFactory(database.ConnectionString);
        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/swagger/v1/swagger.json");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = document.RootElement;
        Assert.True(root.GetProperty("components").TryGetProperty("securitySchemes", out var schemes));
        var bearer = schemes.GetProperty("Bearer");
        Assert.Equal("http", bearer.GetProperty("type").GetString());
        Assert.Equal("bearer", bearer.GetProperty("scheme").GetString());
        foreach (var path in new[] { "/courier/tasks", "/dispatch/shipments" })
        {
            var operation = root.GetProperty("paths").GetProperty(path).GetProperty("get");
            var security = operation.TryGetProperty("security", out var own) ? own : root.GetProperty("security");
            Assert.True(security[0].TryGetProperty("Bearer", out _));
        }
        foreach (var (path, method) in new[] { ("/health", "get"), ("/auth/register", "post"), ("/auth/login", "post") })
        {
            var operation = root.GetProperty("paths").GetProperty(path).GetProperty(method);
            Assert.Equal(0, operation.GetProperty("security").GetArrayLength());
        }
    }
}
