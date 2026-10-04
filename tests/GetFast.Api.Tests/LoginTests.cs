using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace GetFast.Api.Tests;

public sealed class LoginTests(PostgreSqlFixture database) : IClassFixture<PostgreSqlFixture>
{
    [Fact]
    public async Task Login_RegisteredSender_ReturnsServerIdentityRoleAnd30MinuteToken()
    {
        await using var factory = new GetFastApiFactory(database.ConnectionString);
        using var client = factory.CreateClient();
        var email = AuthTestData.Email();
        var password = AuthTestData.Password();
        using var registration = await client.PostAsJsonAsync("/auth/register", new { email, password });
        Assert.Equal(HttpStatusCode.Created, registration.StatusCode);
        using var registered = JsonDocument.Parse(await registration.Content.ReadAsStringAsync());
        using var response = await client.PostAsJsonAsync("/auth/login", new { email = email.ToUpperInvariant(), password });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain(password, content);
        Assert.DoesNotContain(factory.SigningKey, content);
        using var body = JsonDocument.Parse(content);
        Assert.Equal("Bearer", body.RootElement.GetProperty("tokenType").GetString());
        Assert.Equal(1800, body.RootElement.GetProperty("expiresInSeconds").GetInt32());
        using var claims = TokenPayload(body.RootElement.GetProperty("accessToken").GetString()!);
        Assert.Equal(registered.RootElement.GetProperty("id").GetString(), claims.RootElement.GetProperty("sub").GetString());
        Assert.Equal("Sender", claims.RootElement.GetProperty("role").GetString());
        Assert.Equal("GetFast", claims.RootElement.GetProperty("iss").GetString());
        Assert.Equal("GetFast.Api", claims.RootElement.GetProperty("aud").GetString());
        Assert.Equal(1800, claims.RootElement.GetProperty("exp").GetInt64() - claims.RootElement.GetProperty("iat").GetInt64());
    }

    [Fact]
    public async Task Login_WrongPasswordAndUnknownAccount_ReturnSame401()
    {
        await using var factory = new GetFastApiFactory(database.ConnectionString);
        using var client = factory.CreateClient();
        var email = AuthTestData.Email();
        using var registration = await client.PostAsJsonAsync("/auth/register", new { email, password = AuthTestData.Password() });
        Assert.Equal(HttpStatusCode.Created, registration.StatusCode);
        using var wrong = await client.PostAsJsonAsync("/auth/login", new { email, password = AuthTestData.Password() });
        using var missing = await client.PostAsJsonAsync("/auth/login", new { email = AuthTestData.Email(), password = AuthTestData.Password() });
        Assert.Equal(HttpStatusCode.Unauthorized, wrong.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, missing.StatusCode);
        Assert.Equal(await wrong.Content.ReadAsStringAsync(), await missing.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Login_UnknownRoleField_Returns400()
    {
        await using var factory = new GetFastApiFactory(database.ConnectionString);
        using var client = factory.CreateClient();
        using var response = await client.PostAsJsonAsync("/auth/login", new { email = AuthTestData.Email(), password = AuthTestData.Password(), role = "Dispatcher" });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    public static JsonDocument TokenPayload(string token)
    {
        var segment = token.Split('.')[1].Replace('-', '+').Replace('_', '/');
        segment = segment.PadRight((segment.Length + 3) / 4 * 4, '=');
        return JsonDocument.Parse(Encoding.UTF8.GetString(Convert.FromBase64String(segment)));
    }
}
