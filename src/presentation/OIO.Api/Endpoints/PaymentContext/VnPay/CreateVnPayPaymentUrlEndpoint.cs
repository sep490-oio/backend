using System.ComponentModel.DataAnnotations;
using MediatR;
using OIO.Api.Common;
using OIO.Api.Extensions;
using OIO.Application.Abstractions.Payment;
using OIO.Application.Context.PaymentContext.Commands.CreateVnPayPaymentUrl;

namespace OIO.Api.Endpoints.PaymentContext.VnPay;

/// <summary>
/// Tạo URL thanh toán VNPay. Frontend gọi endpoint này để nhận paymentUrl rồi redirect user.
/// </summary>
public sealed class CreateVnPayPaymentUrlEndpoint : IEndpoint
{
    public sealed record Request(
        [Required] decimal Amount,
        [Required] string Currency,
        [Required] string Purpose,
        [Required] string Description,
        string? BankCode = null,
        Guid? AuctionId = null,
        Guid? OrderId = null,
        Guid? BuyNowReservationId = null,
        Guid? PaymentMethodId = null,
        bool SaveCard = false);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoint.Url.VnPay.CreatePaymentUrl, async (
                Request request,
                ISender sender,
                HttpContext httpContext,
                CancellationToken ct) =>
            {
                var command = new CreateVnPayPaymentUrlCommand(
                    Amount: request.Amount,
                    Currency: request.Currency,
                    Purpose: request.Purpose,
                    IpAddress: httpContext.GetIpAddress(),
                    Description: request.Description,
                    BankCode: request.BankCode,
                    AuctionId: request.AuctionId,
                    OrderId: request.OrderId,
                    BuyNowReservationId: request.BuyNowReservationId,
                    PaymentMethodId: request.PaymentMethodId,
                    SaveCard: request.SaveCard);

                var result = await sender.Send(command, ct);

                return result.ToOkHttpResult();
            })
            .RequireAuthorization()
            .WithName(ApiEndpoint.Names.VnPay.CreatePaymentUrl)
            .WithTags(ApiEndpoint.Tags.Payments)
            .Produces(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status400BadRequest);
    }
}
