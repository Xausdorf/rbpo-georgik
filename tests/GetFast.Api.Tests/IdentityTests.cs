using GetFast.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GetFast.Api.Tests;

public sealed class IdentityTests(PostgreSqlFixture database) : IClassFixture<PostgreSqlFixture>
{
    [Fact]
    public async Task Database_HasIdentitySchemaAndOnlyProductRoles()
    {
        await using var factory = new GetFastApiFactory(database.ConnectionString);
        using var client = factory.CreateClient();
        await using var scope = factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<GetFastDbContext>();

        Assert.NotNull(context.Model.FindEntityType("GetFast.Api.Identity.AppUser"));
        Assert.Empty(await context.Database.GetPendingMigrationsAsync());
        var roles = await context.Database.SqlQueryRaw<string>("SELECT \"Name\" AS \"Value\" FROM \"AspNetRoles\"")
            .ToListAsync();
        Assert.Equal(["Courier", "Dispatcher", "Sender"], roles.Order().ToArray());
    }
}
