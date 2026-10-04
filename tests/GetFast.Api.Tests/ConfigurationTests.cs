namespace GetFast.Api.Tests;

public sealed class ConfigurationTests
{
    [Fact]
    public async Task Startup_WithoutDatabaseConnectionString_FailsWithConfigurationError()
    {
        await using var factory = new GetFastApiFactory(string.Empty);

        var exception = Assert.Throws<InvalidOperationException>(() => factory.CreateClient());

        Assert.Contains("ConnectionStrings:DefaultConnection", exception.Message);
    }
}
