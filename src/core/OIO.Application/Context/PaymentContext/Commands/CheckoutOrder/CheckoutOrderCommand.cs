using System.Net;
using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.PaymentContext.Commands.CreateVnPayPaymentUrl;
using OIO.Domain.Context.OrderContext.ValueObjects.Ids;
// using OIO.Domain.Context.PaymentContext.Enums;  // Not needed here, PaymentPurpose is in CreateVnPayPaymentUrl
using OIO.Application.Abstractions.Payment;
using OIO.Domain.Context.OrderContext.Aggregates.Orders;
using OIO.Domain.Context.OrderContext.Errors;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.PaymentContext.Commands.CheckoutOrder;

public sealed record CheckoutOrderCommand(
    Guid OrderId,
    IPAddress IpAddress,
    string? BankCode = null,
    string ReturnUrl = "") : ICommand<CheckoutOrderResponse>;
    
public sealed record CheckoutOrderResponse(
    Guid TransactionId,
    string TransactionRef,
    string PaymentUrl);

internal sealed class CheckoutOrderCommandHandler
    : ICommandHandler<CheckoutOrderCommand, CheckoutOrderResponse>
{
    private readonly IDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly MediatR.ISender _sender;

    public CheckoutOrderCommandHandler(
        IDbContext dbContext,
        IUnitOfWork unitOfWork,
        IClock clock,
        MediatR.ISender sender)
    {
        _dbContext = dbContext;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _sender = sender;
    }

    public async Task<Result<CheckoutOrderResponse, Error>> Handle(
        CheckoutOrderCommand request,
        CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;

        var orderId = OrderId.From(request.OrderId);
        // 1. Lấy thông tin Order
        var order = await _dbContext.GetByIdAsync<Order, OrderId>(
                id: orderId,
                cancellationToken: cancellationToken
        );

        if (order is null)
            return OrderErrors.Order.NotFound(orderId);

        // 2. Thay đổi trạng thái payment của Order thành InitializePayment
        var initResult = order.InitializePayment(now);
        if (initResult.IsFailure)
            return initResult.Error;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // 3. Delegate việc sinh URL cho CreateVnPayPaymentUrlCommand
        var createUrlCommand = new CreateVnPayPaymentUrlCommand(
            Amount: order.Pricing.TotalAmount.Amount,
            Currency: order.Currency,
            Purpose: PaymentPurpose.OrderPayment,
            IpAddress: request.IpAddress,
            Description: $"OrderPayment - Order #{order.OrderNumber.Value}",
            BankCode: request.BankCode,
            AuctionId: order.AuctionId.Value,
            OrderId: order.Id.Value);

        var urlResult = await _sender.Send(createUrlCommand, cancellationToken);

        if (urlResult.IsFailure)
            return urlResult.Error;

        // 4. Trả về response
        return new CheckoutOrderResponse(
            TransactionId: urlResult.Value.TransactionId,
            TransactionRef: urlResult.Value.TransactionRef,
            PaymentUrl: urlResult.Value.PaymentUrl);
    }
}
