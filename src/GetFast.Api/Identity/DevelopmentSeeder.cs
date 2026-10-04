using GetFast.Api.Auth;
using GetFast.Api.Data;
using Microsoft.AspNetCore.Identity;

namespace GetFast.Api.Identity;

public static class DevelopmentSeeder
{
    public static async Task SeedAsync(IServiceProvider services, IConfiguration configuration)
    {
        var courier = ReadCredentials(configuration, RoleNames.Courier);
        var dispatcher = ReadCredentials(configuration, RoleNames.Dispatcher);
        if (string.Equals(courier.Email, dispatcher.Email, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Начальные курьер и диспетчер должны иметь разные email.");

        var database = services.GetRequiredService<GetFastDbContext>();
        await using var transaction = await database.Database.BeginTransactionAsync();
        var users = services.GetRequiredService<UserManager<AppUser>>();
        await CreateEmployeeAsync(users, courier.Email, courier.Password, RoleNames.Courier);
        await CreateEmployeeAsync(users, dispatcher.Email, dispatcher.Password, RoleNames.Dispatcher);
        await transaction.CommitAsync();
    }

    private static (string Email, string Password) ReadCredentials(IConfiguration configuration, string role)
    {
        var email = configuration[$"Seed:{role}Email"];
        var password = configuration[$"Seed:{role}Password"];
        if (!AuthEndpoints.ValidCredentials(email, password))
            throw new InvalidOperationException($"Укажите корректные Seed__{role}Email и Seed__{role}Password для Development.");
        return (email!.Trim(), password!);
    }

    private static async Task CreateEmployeeAsync(UserManager<AppUser> users, string email, string password, string role)
    {
        var user = await users.FindByEmailAsync(email);
        if (user is not null)
        {
            var roles = await users.GetRolesAsync(user);
            if (roles.Count != 1 || roles[0] != role || !await users.CheckPasswordAsync(user, password))
                throw new InvalidOperationException($"Конфликт начальной учётной записи {role}: роль или пароль не совпадает. Автоматическое изменение запрещено.");
            return;
        }

        user = new AppUser { Id = Guid.NewGuid(), Email = email, UserName = email };
        var created = await users.CreateAsync(user, password);
        if (!created.Succeeded) throw new InvalidOperationException($"Не удалось создать начальную учётную запись {role}: проверьте требования к паролю.");
        var assigned = await users.AddToRoleAsync(user, role);
        if (!assigned.Succeeded) throw new InvalidOperationException($"Не удалось назначить роль {role} начальной учётной записи.");
    }
}
