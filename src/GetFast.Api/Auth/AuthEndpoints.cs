using System.ComponentModel.DataAnnotations;
using GetFast.Api.Data;
using GetFast.Api.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace GetFast.Api.Auth;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/auth/register", RegisterAsync)
            .AllowAnonymous()
            .WithName("RegisterSender")
            .WithSummary("Зарегистрировать отправителя")
            .Produces<RegisteredSenderResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem();
        endpoints.MapPost("/auth/login", LoginAsync)
            .AllowAnonymous()
            .WithName("Login")
            .WithSummary("Войти и получить токен на 30 минут")
            .Produces<LoginResponse>()
            .Produces(StatusCodes.Status401Unauthorized)
            .ProducesValidationProblem();
    }

    private static async Task<IResult> RegisterAsync(
        RegisterSenderRequest request, UserManager<AppUser> users, GetFastDbContext database)
    {
        if (!ValidCredentials(request.Email, request.Password))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["credentials"] = ["Нужны корректный email (до 256 символов) и пароль (до 128 символов)."]
            });
        }

        await using var transaction = await database.Database.BeginTransactionAsync();
        var email = request.Email.Trim();
        var user = new AppUser { Id = Guid.NewGuid(), Email = email, UserName = email };
        IdentityResult created;
        try
        {
            created = await users.CreateAsync(user, request.Password);
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException
            { SqlState: PostgresErrorCodes.UniqueViolation, ConstraintName: "UserNameIndex" })
        {
            return IdentityError(IdentityResult.Failed(new IdentityError { Code = "DuplicateUserName" }));
        }
        if (!created.Succeeded)
        {
            return IdentityError(created);
        }

        var assigned = await users.AddToRoleAsync(user, RoleNames.Sender);
        if (!assigned.Succeeded)
        {
            return IdentityError(assigned);
        }

        await transaction.CommitAsync();
        return Results.Json(new RegisteredSenderResponse(user.Id, RoleNames.Sender),
            statusCode: StatusCodes.Status201Created);
    }

    internal static bool ValidCredentials(string? email, string? password) =>
        !string.IsNullOrWhiteSpace(email) && email.Length <= 256 &&
        new EmailAddressAttribute().IsValid(email.Trim()) &&
        !string.IsNullOrEmpty(password) && password.Length <= 128;

    private static async Task<IResult> LoginAsync(LoginRequest request, UserManager<AppUser> users, JwtTokenService tokens)
    {
        if (!ValidCredentials(request.Email, request.Password))
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["credentials"] = ["Нужны корректные email и пароль."] });

        var user = await users.FindByEmailAsync(request.Email.Trim());
        if (user is null || !await users.CheckPasswordAsync(user, request.Password))
            return Results.Unauthorized();

        return Results.Ok(tokens.Create(user, await users.GetRolesAsync(user)));
    }

    private static IResult IdentityError(IdentityResult result) => Results.ValidationProblem(
        result.Errors.GroupBy(error => error.Code)
            .ToDictionary(group => group.Key, _ => new[] { "Учётная запись не создана: проверьте email и требования к паролю." }));
}
