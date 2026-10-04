using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using GetFast.Api.Auth;
using GetFast.Api.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace GetFast.Api.Tests;

public sealed class AuthorizationTests(PostgreSqlFixture database) : IClassFixture<PostgreSqlFixture>
{
    [Theory]
    [InlineData("Sender", "/dispatch/shipments", HttpStatusCode.Forbidden)]
    [InlineData("Courier", "/dispatch/shipments", HttpStatusCode.Forbidden)]
    [InlineData("Dispatcher", "/dispatch/shipments", HttpStatusCode.OK)]
    [InlineData("Sender", "/courier/tasks", HttpStatusCode.Forbidden)]
    [InlineData("Courier", "/courier/tasks", HttpStatusCode.OK)]
    [InlineData("Dispatcher", "/courier/tasks", HttpStatusCode.Forbidden)]
    public async Task RoleEndpoint_AllRoles_EnforcesGroupPolicy(string role, string path, HttpStatusCode expected)
    {
        await using var factory = new GetFastApiFactory(database.ConnectionString);
        using var client = factory.CreateClient();
        string email, password;
        if (role == "Sender")
        {
            email = AuthTestData.Email(); password = AuthTestData.Password();
            using var registered = await client.PostAsJsonAsync("/auth/register", new { email, password });
            Assert.Equal(HttpStatusCode.Created, registered.StatusCode);
        }
        else if (role == "Courier") { email = factory.CourierEmail; password = factory.CourierPassword; }
        else { email = factory.DispatcherEmail; password = factory.DispatcherPassword; }
        using var login = await client.PostAsJsonAsync("/auth/login", new { email, password });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        using var body = JsonDocument.Parse(await login.Content.ReadAsStringAsync());
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body.RootElement.GetProperty("accessToken").GetString());
        using var response = await client.GetAsync(path);
        Assert.Equal(expected, response.StatusCode);
        if (expected == HttpStatusCode.OK) Assert.Equal("[]", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Endpoints_HaveRolePolicyOrExplicitAllowedAnonymousAccess()
    {
        await using var factory = new GetFastApiFactory(database.ConnectionString);
        using var client = factory.CreateClient();
        var endpoints = factory.Services.GetRequiredService<EndpointDataSource>().Endpoints.OfType<RouteEndpoint>().ToArray();
        Assert.NotEmpty(endpoints);
        var policyProvider = factory.Services.GetRequiredService<IAuthorizationPolicyProvider>();
        foreach (var endpoint in endpoints)
        {
            await AssertEndpointPolicyAsync(endpoint, policyProvider);
            if (endpoint.Metadata.GetMetadata<IAllowAnonymous>() is null)
            {
                foreach (var method in endpoint.Metadata.GetMetadata<HttpMethodMetadata>()!.HttpMethods)
                {
                    using var response = await client.SendAsync(new HttpRequestMessage(new HttpMethod(method), endpoint.RoutePattern.RawText));
                    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
                }
            }
        }
    }

    [Theory]
    [InlineData("expired")]
    [InlineData("future")]
    [InlineData("signature")]
    [InlineData("issuer")]
    [InlineData("audience")]
    [InlineData("algorithm")]
    [InlineData("unsigned")]
    [InlineData("no-expiry")]
    public async Task InvalidToken_Returns401WithBearerChallenge(string defect)
    {
        await using var factory = new GetFastApiFactory(database.ConnectionString);
        using var client = factory.CreateClient();
        var key = defect == "signature" ? Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N") : factory.SigningKey;
        var now = DateTime.UtcNow;
        var token = new JwtSecurityToken(
            defect == "issuer" ? "Other" : JwtSettings.Issuer,
            defect == "audience" ? "Other" : JwtSettings.Audience,
            [new Claim("sub", Guid.NewGuid().ToString()), new Claim("role", "Dispatcher")],
            notBefore: defect == "future" ? now.AddMinutes(1) : now.AddMinutes(-2),
            expires: defect == "no-expiry" ? null : defect == "expired" ? now.AddSeconds(-1) : now.AddMinutes(30),
            signingCredentials: defect == "unsigned" ? null : new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), defect == "algorithm" ? SecurityAlgorithms.HmacSha384 : SecurityAlgorithms.HmacSha256));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", new JwtSecurityTokenHandler().WriteToken(token));
        using var response = await client.GetAsync("/dispatch/shipments");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Contains(response.Headers.WwwAuthenticate, challenge => challenge.Scheme == "Bearer");
    }

    [Fact]
    public async Task Fallback_ProtectsMethodWithoutPolicy_AndAuditDetectsIt()
    {
        await using var factory = new GetFastApiFactory(database.ConnectionString);
        await using var probeFactory = factory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
            services.AddSingleton<IStartupFilter, ProbeEndpointStartupFilter>()));
        using var client = probeFactory.CreateClient();
        using var anonymous = await client.GetAsync("/test/fallback");
        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
            probeFactory.Services.GetRequiredService<JwtTokenService>().Create(new AppUser { Id = Guid.NewGuid() }, [RoleNames.Sender]).AccessToken);
        using var authenticated = await client.GetAsync("/test/fallback");
        Assert.Equal(HttpStatusCode.OK, authenticated.StatusCode);
        var endpoint = probeFactory.Services.GetRequiredService<EndpointDataSource>().Endpoints.OfType<RouteEndpoint>()
            .Single(endpoint => endpoint.RoutePattern.RawText == "/test/fallback");
        await Assert.ThrowsAnyAsync<Exception>(() => AssertEndpointPolicyAsync(endpoint,
            probeFactory.Services.GetRequiredService<IAuthorizationPolicyProvider>()));
    }

    private static async Task AssertEndpointPolicyAsync(RouteEndpoint endpoint, IAuthorizationPolicyProvider provider)
    {
        var path = endpoint.RoutePattern.RawText!;
        if (endpoint.Metadata.GetMetadata<IAllowAnonymous>() is not null)
        {
            Assert.True(path is "/health" or "/auth/register" or "/auth/login" || path.StartsWith("/track/", StringComparison.Ordinal), $"Анонимный доступ запрещён: {path}");
            return;
        }
        var policies = endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>();
        Assert.NotEmpty(policies);
        var combined = await AuthorizationPolicy.CombineAsync(provider, policies);
        Assert.NotNull(combined);
        Assert.Contains(combined.Requirements, requirement => requirement is RolesAuthorizationRequirement roles &&
            roles.AllowedRoles.Any(role => role is RoleNames.Sender or RoleNames.Courier or RoleNames.Dispatcher));
    }

    private sealed class ProbeEndpointStartupFilter : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
        {
            next(app);
            app.UseEndpoints(endpoints => endpoints.MapGet("/test/fallback", () => Results.Ok()));
        };
    }
}
