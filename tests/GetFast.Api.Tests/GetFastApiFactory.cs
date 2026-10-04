using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace GetFast.Api.Tests;

public sealed class GetFastApiFactory(
    string connectionString,
    string environment = "Development",
    Dictionary<string, string?>? settings = null) : WebApplicationFactory<Program>
{
    public string SigningKey { get; } = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");
    public string CourierEmail { get; } = AuthTestData.Email();
    public string CourierPassword { get; } = AuthTestData.Password();
    public string DispatcherEmail { get; } = AuthTestData.Email();
    public string DispatcherPassword { get; } = AuthTestData.Password();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(environment);
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = connectionString,
                ["Jwt:SigningKey"] = SigningKey,
                ["Seed:CourierEmail"] = CourierEmail,
                ["Seed:CourierPassword"] = CourierPassword,
                ["Seed:DispatcherEmail"] = DispatcherEmail,
                ["Seed:DispatcherPassword"] = DispatcherPassword
            });
            if (settings is not null) configuration.AddInMemoryCollection(settings);
        });
    }
}
