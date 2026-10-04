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

    public Task InitializeAsync() => _database.StartAsync();

    public Task DisposeAsync() => _database.DisposeAsync().AsTask();
}
