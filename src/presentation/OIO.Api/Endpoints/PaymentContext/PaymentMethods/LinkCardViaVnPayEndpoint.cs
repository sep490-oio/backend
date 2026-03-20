using MediatR;
using OIO.Api.Common;
using OIO.Api.Extensions;
using OIO.Application.Context.PaymentContext.Commands.PaymentMethods;

namespace OIO.Api.Endpoints.PaymentContext.PaymentMethods;

/// <summary>
/// Tạo URL VNPay để link thẻ (token_create). User redirect → nhập thẻ → OTP → callback tạo PaymentMethod.
/// </summary>
public sealed class LinkCardViaVnPayEndpoint : IEndpoint
{
    public sealed record Request(string? CardType = null);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoint.Url.Payments.LinkCard, async (
                Request request,
                ISender sender,
                CancellationToken ct) =>
            {
                var command = new LinkCardViaVnPayCommand(CardType: request.CardType);
                var result = await sender.Send(command, ct);
                return result.ToOkHttpResult();
            })
            .RequireAuthorization()
            .WithName(ApiEndpoint.Names.Payments.LinkCardViaVnPay)
            .WithTags(ApiEndpoint.Tags.Payments)
            .Produces<LinkCardViaVnPayResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest);
    }
}
