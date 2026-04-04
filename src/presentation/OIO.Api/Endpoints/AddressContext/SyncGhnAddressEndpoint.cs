using MediatR;
using OIO.Api.Common;
using OIO.Application.Abstractions.Address;
using OIO.Application.Context.AddressContext.Commands.SyncGhnAddress;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.AddressContext;

public sealed class SyncGhnAddressEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoint.Url.Address.Sync, async (
                ISender sender,
                CancellationToken ct) =>
            {
                var result = await sender.Send(new SyncGhnAddressCommand(), ct);
                return result.ToOkHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Warehouse.SyncAddress)
            .WithName(ApiEndpoint.Names.Address.SyncGhnAddress)
            .WithTags(ApiEndpoint.Tags.Address)
            .Produces<GhnSyncResult>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden);
    }
}
