namespace GetFast.Api.Auth;

public sealed record RegisterSenderRequest(string Email, string Password);
public sealed record RegisteredSenderResponse(Guid Id, string Role);
public sealed record LoginRequest(string Email, string Password);
public sealed record LoginResponse(string AccessToken, string TokenType, int ExpiresInSeconds);
