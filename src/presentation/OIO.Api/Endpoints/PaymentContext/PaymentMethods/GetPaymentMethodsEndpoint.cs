using MediatR;
using Microsoft.Extensions.Logging;
using OIO.Api.Common;
using OIO.Application.Context.PaymentContext.DTOs;
using OIO.Application.Context.PaymentContext.Queries.GetMyPaymentMethods;

namespace OIO.Api.Endpoints.PaymentContext.PaymentMethods;

public sealed class GetPaymentMethodsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiEndpoint.Url.Payments.GetMethods, async (
                string? status,
                ISender sender,
                ILogger<GetPaymentMethodsEndpoint> logger,
                CancellationToken ct) =>
            {
                PaymentMethodStatusFilter? filter = null;

                if (!string.IsNullOrWhiteSpace(status))
                {
                    if (Enum.TryParse<PaymentMethodStatusFilter>(status, ignoreCase: true, out var parsed))
                    {
                        filter = parsed;
                    }
                    else
                    {
                        logger.LogWarning(
                            "Unknown PaymentMethodStatusFilter value '{StatusValue}'; falling back to Active.",
                            status);
                        filter = PaymentMethodStatusFilter.Active;
                    }
                }

                var result = await sender.Send(new GetMyPaymentMethodsQuery(filter), ct);
                return result.ToOkHttpResult();
            })
            .RequireAuthorization()
            .WithName(ApiEndpoint.Names.Payments.GetPaymentMethods)
            .WithTags(ApiEndpoint.Tags.Payments)
            .Produces<IReadOnlyList<PaymentMethodDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized);
    }
}
