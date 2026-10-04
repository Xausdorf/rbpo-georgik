using GetFast.Api.Identity;

namespace GetFast.Api.Endpoints;

public sealed record RoleEndpointGroups(RouteGroupBuilder Shipments, RouteGroupBuilder Courier, RouteGroupBuilder Dispatch);

public static class RoleEndpoints
{
    public static RoleEndpointGroups MapRoleEndpointGroups(this IEndpointRouteBuilder endpoints)
    {
        var groups = new RoleEndpointGroups(
            endpoints.MapGroup("/shipments").RequireAuthorization(RoleNames.Sender),
            endpoints.MapGroup("/courier").RequireAuthorization(RoleNames.Courier),
            endpoints.MapGroup("/dispatch").RequireAuthorization(RoleNames.Dispatcher));

        groups.Courier.MapGet("/tasks", () => Results.Ok(Array.Empty<object>()))
            .WithName("CourierTasks").WithSummary("Проверить доступ курьера: список пока пуст")
            .Produces<object[]>();
        groups.Dispatch.MapGet("/shipments", () => Results.Ok(Array.Empty<object>()))
            .WithName("DispatchShipments").WithSummary("Проверить доступ диспетчера: список пока пуст")
            .Produces<object[]>();
        return groups;
    }
}
