using GetFast.Api.Data;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace GetFast.Api.Tests;

public sealed class PostgreSqlFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _database = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("getfast_tests")
        .WithUsername("getfast_test")
        .WithPassword(Guid.NewGuid().ToString("N"))
        .Build();

    public string ConnectionString => _database.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _database.StartAsync();
        await using var context = new GetFastDbContext(new DbContextOptionsBuilder<GetFastDbContext>()
            .UseNpgsql(ConnectionString).Options);
        await context.Database.MigrateAsync();
    }

    public Task DisposeAsync() => _database.DisposeAsync().AsTask();
}
