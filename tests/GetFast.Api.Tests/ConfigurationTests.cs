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

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
    [InlineData("яяяяяяяяяяяяяяя")]
    public async Task Startup_MissingOrShortSigningKey_FailsBeforeConnectingToDatabase(string? key)
    {
        await using var factory = new GetFastApiFactory("Host=invalid", "Production",
            new Dictionary<string, string?> { ["Jwt:SigningKey"] = key });
        var exception = Assert.Throws<InvalidOperationException>(() => factory.CreateClient());
        Assert.Contains("Jwt__SigningKey", exception.Message);
        if (!string.IsNullOrEmpty(key)) Assert.DoesNotContain(key, exception.Message);
    }

    [Fact]
    public async Task Startup_Exactly32Utf8Bytes_AllowsProductionStartup()
    {
        await using var factory = new GetFastApiFactory("Host=invalid", "Production",
            new Dictionary<string, string?> { ["Jwt:SigningKey"] = new string('я', 16) });
        using var client = factory.CreateClient();
        Assert.NotNull(client);
    }
}
