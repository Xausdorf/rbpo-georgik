using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using GetFast.Api.Identity;
using Microsoft.IdentityModel.Tokens;

namespace GetFast.Api.Auth;

public sealed class JwtTokenService(JwtSettings settings)
{
    public LoginResponse Create(AppUser user, IEnumerable<string> roles)
    {
        var now = DateTime.UtcNow;
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
            new(JwtRegisteredClaimNames.Iat, new DateTimeOffset(now).ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture), ClaimValueTypes.Integer64)
        };
        claims.AddRange(roles.Select(role => new Claim("role", role)));
        var token = new JwtSecurityToken(JwtSettings.Issuer, JwtSettings.Audience, claims,
            notBefore: now, expires: now.AddSeconds(JwtSettings.LifetimeSeconds),
            signingCredentials: new SigningCredentials(settings.SigningKey, SecurityAlgorithms.HmacSha256));
        return new LoginResponse(new JwtSecurityTokenHandler().WriteToken(token), "Bearer", JwtSettings.LifetimeSeconds);
    }
}
