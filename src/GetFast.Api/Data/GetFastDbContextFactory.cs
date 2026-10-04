using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace GetFast.Api.Data;

public sealed class GetFastDbContextFactory : IDesignTimeDbContextFactory<GetFastDbContext>
{
    public GetFastDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Укажите ConnectionStrings__DefaultConnection для команд EF Core.");
        }

        return new GetFastDbContext(new DbContextOptionsBuilder<GetFastDbContext>()
            .UseNpgsql(connectionString).Options);
    }
}
