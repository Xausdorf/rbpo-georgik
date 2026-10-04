using System.Net;
using System.Net.Http.Json;
using GetFast.Api.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace GetFast.Api.Tests;

public sealed class SeedingTests(PostgreSqlFixture database) : IClassFixture<PostgreSqlFixture>
{
    [Fact]
    public async Task Development_CreatesConfiguredEmployees_AndRestartKeepsThem()
    {
        await using var first = new GetFastApiFactory(database.ConnectionString);
        using var firstClient = first.CreateClient();
        await using var scope = first.Services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var courier = await users.FindByEmailAsync(first.CourierEmail);
        var dispatcher = await users.FindByEmailAsync(first.DispatcherEmail);
        Assert.NotNull(courier);
        Assert.NotNull(dispatcher);
        Assert.Equal(["Courier"], await users.GetRolesAsync(courier));
        Assert.Equal(["Dispatcher"], await users.GetRolesAsync(dispatcher));
        var settings = new Dictionary<string, string?>
        {
            ["Seed:CourierEmail"] = first.CourierEmail, ["Seed:CourierPassword"] = first.CourierPassword,
            ["Seed:DispatcherEmail"] = first.DispatcherEmail, ["Seed:DispatcherPassword"] = first.DispatcherPassword
        };
        await using var second = new GetFastApiFactory(database.ConnectionString, settings: settings);
        using var secondClient = second.CreateClient();
        await using var secondScope = second.Services.CreateAsyncScope();
        var restartedUsers = secondScope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        Assert.Equal(courier.Id, (await restartedUsers.FindByEmailAsync(first.CourierEmail))!.Id);
        Assert.Equal(courier.PasswordHash, (await restartedUsers.FindByEmailAsync(first.CourierEmail))!.PasswordHash);
        foreach (var credentials in new[] { (first.CourierEmail, first.CourierPassword), (first.DispatcherEmail, first.DispatcherPassword) })
        {
            using var login = await secondClient.PostAsJsonAsync("/auth/login", new { email = credentials.Item1, password = credentials.Item2 });
            Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        }
    }

    [Fact]
    public async Task Production_DoesNotCreateConfiguredEmployees()
    {
        await using var factory = new GetFastApiFactory(database.ConnectionString, "Production");
        using var client = factory.CreateClient();
        await using var scope = factory.Services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        Assert.Null(await users.FindByEmailAsync(factory.CourierEmail));
        Assert.Null(await users.FindByEmailAsync(factory.DispatcherEmail));
    }

    [Fact]
    public async Task Development_SeedEmailBelongsToSender_RefusesPromotion()
    {
        await using var first = new GetFastApiFactory(database.ConnectionString);
        using var client = first.CreateClient();
        var email = AuthTestData.Email();
        var password = AuthTestData.Password();
        using var registration = await client.PostAsJsonAsync("/auth/register", new { email, password });
        Assert.Equal(HttpStatusCode.Created, registration.StatusCode);
        await using var conflicting = new GetFastApiFactory(database.ConnectionString, settings:
            new Dictionary<string, string?> { ["Seed:CourierEmail"] = email, ["Seed:CourierPassword"] = password });
        Assert.Throws<InvalidOperationException>(() => conflicting.CreateClient());
        await using var scope = first.Services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        Assert.Equal(["Sender"], await users.GetRolesAsync((await users.FindByEmailAsync(email))!));
    }

    [Fact]
    public async Task Development_MissingSeedPassword_FailsWithoutRevealingSecrets()
    {
        await using var factory = new GetFastApiFactory(database.ConnectionString, settings:
            new Dictionary<string, string?> { ["Seed:CourierPassword"] = null });
        var exception = Assert.Throws<InvalidOperationException>(() => factory.CreateClient());
        Assert.Contains("Seed__Courier", exception.Message);
        Assert.DoesNotContain(factory.DispatcherPassword, exception.Message);
    }
}
