using GetFast.Api.Identity;
using Microsoft.AspNetCore.Authorization;

namespace GetFast.Api.Auth;

public static class AuthorizationSetup
{
    public static void AddGetFastAuthorization(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
            options.AddPolicy(RoleNames.Sender, policy => policy.RequireRole(RoleNames.Sender));
            options.AddPolicy(RoleNames.Courier, policy => policy.RequireRole(RoleNames.Courier));
            options.AddPolicy(RoleNames.Dispatcher, policy => policy.RequireRole(RoleNames.Dispatcher));
        });
    }
}
