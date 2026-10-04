namespace GetFast.Api.Tests;

public static class AuthTestData
{
    public static string Email() => $"test-{Guid.NewGuid():N}@example.invalid";
    public static string Password() => $"Aa1!{Guid.NewGuid():N}";
}
