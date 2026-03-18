using MediatR;
using OIO.Api.Common;
using OIO.Api.Extensions;
using OIO.Application.Abstractions.Payment;
using OIO.Application.Context.PaymentContext.Commands.CreateVnPayPaymentUrl;

namespace OIO.Api.Endpoints.PaymentContext.VnPay;

/// <summary>
/// Tạo URL thanh toán VNPay. Frontend gọi endpoint này → nhận paymentUrl → redirect user.
/// </summary>
public sealed class CreateVnPayPaymentUrlEndpoint : IEndpoint
{
    public sealed record Request(
        decimal Amount,
        string Currency,
        string Purpose,
        string Description,
        string? BankCode = null,
        Guid? AuctionId = null,
        Guid? OrderId = null);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoint.Url.VnPay.CreatePaymentUrl, async (
                Request request,
                ISender sender,
                HttpContext httpContext,
                CancellationToken ct) =>
            {
                var purpose = Enum.TryParse<PaymentPurpose>(request.Purpose, true, out var p)
                    ? p
                    : PaymentPurpose.WalletTopUp;

                var command = new CreateVnPayPaymentUrlCommand(
                    Amount: request.Amount,
                    Currency: request.Currency,
                    Purpose: purpose,
                    IpAddress: httpContext.GetIpAddress(),
                    Description: request.Description,
                    BankCode: request.BankCode,
                    AuctionId: request.AuctionId,
                    OrderId: request.OrderId);

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
