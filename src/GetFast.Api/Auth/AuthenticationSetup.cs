using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace GetFast.Api.Auth;

public static class AuthenticationSetup
{
    public static void AddGetFastAuthentication(this IServiceCollection services)
    {
        services.AddSingleton<JwtSettings>();
        services.AddSingleton<JwtTokenService>();
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<JwtSettings>((options, settings) =>
            {
                options.MapInboundClaims = false;
                options.IncludeErrorDetails = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true, ValidIssuer = JwtSettings.Issuer,
                    ValidateAudience = true, ValidAudience = JwtSettings.Audience,
                    ValidateIssuerSigningKey = true, IssuerSigningKey = settings.SigningKey,
                    ValidateLifetime = true, RequireExpirationTime = true,
                    RequireSignedTokens = true, ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
                    ClockSkew = TimeSpan.Zero,
                    NameClaimType = JwtRegisteredClaimNames.Sub, RoleClaimType = "role"
                };
            });
    }
}
