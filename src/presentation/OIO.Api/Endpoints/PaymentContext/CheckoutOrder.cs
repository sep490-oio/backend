using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using OIO.Application.Context.PaymentContext.Commands.CheckoutOrder;
using OIO.Api.Common;
using OIO.Api.Extensions;

namespace OIO.Api.Endpoints.PaymentContext;

public class CheckoutOrderEndpoint : IEndpoint
{
    public sealed record Request(
        Guid OrderId,
        string? BankCode,
        string? ReturnUrl);
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoint.Url.Payments.CheckoutOrder, async (Request request,
                ISender sender,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var ipAddress = httpContext.GetIpAddress();

                var command = new CheckoutOrderCommand(
                    OrderId: request.OrderId,
                    IpAddress: ipAddress,
                    BankCode: request.BankCode,
                    ReturnUrl: request.ReturnUrl ?? string.Empty
                );

                var result = await sender.Send(command, cancellationToken);

                return result.ToOkHttpResult();
            })
            .RequireAuthorization()
            .WithName(ApiEndpoint.Names.Payments.CheckoutOrder)
            .WithTags(ApiEndpoint.Tags.Payments);
    }
}


