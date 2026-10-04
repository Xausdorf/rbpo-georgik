using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GetFast.Api.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace GetFast.Api.Tests;

public sealed class RegistrationTests(PostgreSqlFixture database) : IClassFixture<PostgreSqlFixture>
{
    [Fact]
    public async Task Register_ValidRequest_CreatesOnlySenderWithHashedPassword()
    {
        var email = AuthTestData.Email();
        var password = AuthTestData.Password();
        await using var factory = new GetFastApiFactory(database.ConnectionString);
        using var client = factory.CreateClient();
        using var response = await client.PostAsJsonAsync("/auth/register", new { email, password });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain(password, content);
        using var body = JsonDocument.Parse(content);
        Assert.Equal("Sender", body.RootElement.GetProperty("role").GetString());
        await using var scope = factory.Services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var user = await users.FindByEmailAsync(email);
        Assert.NotNull(user);
        Assert.Equal(user.Id.ToString(), body.RootElement.GetProperty("id").GetString());
        Assert.Equal(["Sender"], await users.GetRolesAsync(user));
        Assert.NotEqual(password, user.PasswordHash);
        Assert.True(await users.CheckPasswordAsync(user, password));
    }

    [Theory]
    [InlineData("role", "Dispatcher")]
    [InlineData("role", "Courier")]
    [InlineData("Role", "dispatcher")]
    [InlineData("isAdmin", "true")]
    public async Task Register_UnknownField_RejectsAndDoesNotCreateUser(string field, string value)
    {
        var email = AuthTestData.Email();
        await using var factory = new GetFastApiFactory(database.ConnectionString);
        using var client = factory.CreateClient();
        using var response = await client.PostAsJsonAsync("/auth/register", new Dictionary<string, string>
        {
            ["email"] = email, ["password"] = AuthTestData.Password(), [field] = value
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await using var scope = factory.Services.CreateAsyncScope();
        Assert.Null(await scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>().FindByEmailAsync(email));
    }

    [Theory]
    [InlineData("null", "valid")]
    [InlineData("invalid", "valid")]
    [InlineData("oversized", "valid")]
    [InlineData("valid", "null")]
    [InlineData("valid", "weak")]
    [InlineData("valid", "oversized")]
    public async Task Register_InvalidCredentials_Returns400(string emailKind, string passwordKind)
    {
        string? email = emailKind switch { "null" => null, "invalid" => "invalid", "oversized" => new string('a', 257) + "@example.invalid", _ => AuthTestData.Email() };
        string? password = passwordKind switch { "null" => null, "weak" => "a", "oversized" => new string('A', 129) + "a1!", _ => AuthTestData.Password() };
        await using var factory = new GetFastApiFactory(database.ConnectionString);
        using var client = factory.CreateClient();
        using var response = await client.PostAsJsonAsync("/auth/register", new { email, password });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_DuplicateEmailIgnoringCase_RejectsSecondRequest()
    {
        var email = AuthTestData.Email();
        await using var factory = new GetFastApiFactory(database.ConnectionString);
        using var client = factory.CreateClient();
        using var first = await client.PostAsJsonAsync("/auth/register", new { email, password = AuthTestData.Password() });
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        using var second = await client.PostAsJsonAsync("/auth/register", new { email = email.ToUpperInvariant(), password = AuthTestData.Password() });
        Assert.Equal(HttpStatusCode.BadRequest, second.StatusCode);
    }
}
