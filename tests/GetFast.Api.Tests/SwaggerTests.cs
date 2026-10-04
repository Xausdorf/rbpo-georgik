using System.Net;
using System.Net.Http.Headers;
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
}
