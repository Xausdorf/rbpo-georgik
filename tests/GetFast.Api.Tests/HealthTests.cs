using System.Net;
using System.Text.Json;
using Npgsql;

namespace GetFast.Api.Tests;

public sealed class HealthTests(PostgreSqlFixture database) : IClassFixture<PostgreSqlFixture>
{
    [Fact]
    public async Task Health_WithAvailableDatabase_ReturnsHealthy()
    {
        await using var factory = new GetFastApiFactory(database.ConnectionString);
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("healthy", body.RootElement.GetProperty("status").GetString());
    }

    [Fact]
    public async Task Health_WithRejectedDatabaseConnection_Returns503WithoutSecrets()
    {
        var password = Guid.NewGuid().ToString("N");
        var connectionString = new NpgsqlConnectionStringBuilder(database.ConnectionString)
        {
            Password = password,
            Timeout = 3
        }.ConnectionString;
        await using var factory = new GetFastApiFactory(connectionString);
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        using var body = JsonDocument.Parse(content);
        Assert.Equal("unhealthy", body.RootElement.GetProperty("status").GetString());
        Assert.DoesNotContain(password, content);
        Assert.DoesNotContain(connectionString, content);
    }
}
