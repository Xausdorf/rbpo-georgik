using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace GetFast.Api.Auth;

public sealed class JwtSettings
{
    public const string Issuer = "GetFast";
    public const string Audience = "GetFast.Api";
    public const int LifetimeSeconds = 30 * 60;
    public SymmetricSecurityKey SigningKey { get; }

    public JwtSettings(IConfiguration configuration)
    {
        var value = configuration["Jwt:SigningKey"];
        if (string.IsNullOrWhiteSpace(value) || Encoding.UTF8.GetByteCount(value) < 32)
        {
            throw new InvalidOperationException("Укажите Jwt__SigningKey: случайный ключ длиной не менее 32 байт UTF-8.");
        }

        SigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(value));
    }
}
