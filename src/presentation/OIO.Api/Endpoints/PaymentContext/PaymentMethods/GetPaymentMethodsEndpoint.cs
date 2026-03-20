using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.PaymentContext.DTOs;
using OIO.Application.Context.PaymentContext.Queries.GetMyPaymentMethods;

namespace OIO.Api.Endpoints.PaymentContext.PaymentMethods;

public sealed class GetPaymentMethodsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiEndpoint.Url.Payments.GetMethods, async (
                ISender sender,
                CancellationToken ct) =>
            {
                var result = await sender.Send(new GetMyPaymentMethodsQuery(), ct);
                return result.ToOkHttpResult();
            })
            .RequireAuthorization()
            .WithName(ApiEndpoint.Names.Payments.GetPaymentMethods)
            .WithTags(ApiEndpoint.Tags.Payments)
            .Produces<IReadOnlyList<PaymentMethodDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized);
    }
}
