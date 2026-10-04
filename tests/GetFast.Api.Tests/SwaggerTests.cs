using System.Net;

namespace GetFast.Api.Tests;

public sealed class SwaggerTests(PostgreSqlFixture database) : IClassFixture<PostgreSqlFixture>
{
    [Theory]
    [InlineData("Development", "/swagger/index.html", HttpStatusCode.OK)]
    [InlineData("Development", "/swagger/v1/swagger.json", HttpStatusCode.OK)]
    [InlineData("Production", "/swagger/index.html", HttpStatusCode.NotFound)]
    [InlineData("Production", "/swagger/v1/swagger.json", HttpStatusCode.NotFound)]
    public async Task Swagger_IsAvailableOnlyInDevelopment(
        string environment, string path, HttpStatusCode expectedStatus)
    {
        await using var factory = new GetFastApiFactory(database.ConnectionString, environment);
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(path);

        Assert.Equal(expectedStatus, response.StatusCode);
    }
}
