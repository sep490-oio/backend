using OIO.Api.Common;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.UserContext.Me;

/// <summary>
/// GET /api/me/orders/seller-direct-ship/shipping-provider-options
/// Returns the list of active shipping providers that a seller may pick when
/// booking an outbound shipment via <c>BookOutboundShipmentEndpoint</c>.
/// Excludes the "external" provider because ship-outside is handled via the
/// dedicated SelfShipOrder flow, not this endpoint.
///
/// Response shape is kept intentionally thin — no credentials or internal
/// configuration is exposed.
/// </summary>
public sealed class GetSellerShippingProviderOptionsEndpoint : IEndpoint
{
    public sealed record ProviderOption(string Code, string DisplayName, bool IsDefault);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiEndpoint.Url.Me.GetSellerShippingProviderOptions, () =>
            {
                // V1 scope: a static list of supported providers, excluding "external".
                // When a provider registry becomes available this list should read
                // from it at runtime; keeping it local avoids leaking warehouse-only
                // permissions to the seller UI.
                var options = new List<ProviderOption>
                {
                    new("ghn", "Giao Hàng Nhanh", IsDefault: true),
                    new("ghtk", "Giao Hàng Tiết Kiệm", IsDefault: false),
                    new("viettel_post", "Viettel Post", IsDefault: false),
                };
                return Results.Ok(options);
            })
            .RequireAuthorization(App.Permissions.Catalogs.Me.ReadDirectShipOrders)
            .WithName(ApiEndpoint.Names.Me.GetSellerShippingProviderOptions)
            .WithTags(ApiEndpoint.Tags.Me)
            .Produces<List<ProviderOption>>(StatusCodes.Status200OK);
    }
}
