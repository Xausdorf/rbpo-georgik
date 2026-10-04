using GetFast.Api.Data;

namespace GetFast.Api.Endpoints;

public static class HealthEndpoints
{
    public static IEndpointConventionBuilder MapHealthEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/health", async (GetFastDbContext database, CancellationToken cancellationToken) =>
        {
            var canConnect = await database.Database.CanConnectAsync(cancellationToken);
            var response = new HealthResponse(canConnect ? "healthy" : "unhealthy");

            return Results.Json(response, statusCode: canConnect
                ? StatusCodes.Status200OK
                : StatusCodes.Status503ServiceUnavailable);
        })
        .AllowAnonymous()
        .WithName("Health")
        .WithSummary("Проверить доступность API и PostgreSQL")
        .Produces<HealthResponse>()
        .Produces<HealthResponse>(StatusCodes.Status503ServiceUnavailable);
    }
}

public sealed record HealthResponse(string Status);
