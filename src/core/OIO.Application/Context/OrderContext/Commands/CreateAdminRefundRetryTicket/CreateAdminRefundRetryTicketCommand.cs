using CSharpFunctionalExtensions;
using Microsoft.Extensions.Logging;
using OIO.Application.Abstractions.Messaging;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.OrderContext.Commands.CreateAdminRefundRetryTicket;

/// <summary>
/// D1 compensating-path command. Fires from <c>ConfirmOrderReturnReceivedCommandHandler</c>
/// when <c>RefundBuyerAsync</c> fails AFTER <see cref="Domain.Context.OrderContext.Aggregates.Orders.OrderReturn.Resolve"/>
/// has committed. Creates an admin-queue ticket so the stuck refund surfaces for
/// manual retry via <c>RetryDeferredRefundCommand</c>.
/// </summary>
/// <remarks>
/// V1 scope: no dedicated <c>admin_refund_retry_tickets</c> table exists yet (deferred
/// to Phase E migration). Handler emits a structured ERROR-level log with the full
/// context and returns success so the compensating path completes. Once the table
/// exists, the handler inserts a row. Admin UI queries the log / table via
/// out-of-band operational tooling until the FE surface lands.
/// </remarks>
public sealed record CreateAdminRefundRetryTicketCommand(
    Guid OrderId,
    Guid OrderReturnId,
    string Intent,
    decimal? Amount,
    string FailureReason) : ICommand, IHasValidate
{
    public ViolationsError Validate() =>
        CreateAdminRefundRetryTicketCommand.Check()
            .WithOwnerName("CreateAdminRefundRetryTicket")
            .Field(OrderId).NotEmptyGuid()
            .Field(OrderReturnId).NotEmptyGuid()
            .Field(Intent).NotWhiteSpace()
            .Field(FailureReason).NotWhiteSpace();
}

internal sealed class CreateAdminRefundRetryTicketCommandHandler(
    ILogger<CreateAdminRefundRetryTicketCommandHandler> logger)
    : ICommandHandler<CreateAdminRefundRetryTicketCommand>
{
    public Task<UnitResult<Error>> Handle(
        CreateAdminRefundRetryTicketCommand request,
        CancellationToken cancellationToken)
    {
        // V1: persist as a structured log only. Phase E adds the dedicated table +
        // row insert. Key fields embedded so existing log aggregation surfaces the
        // stuck refund without additional infrastructure.
        logger.LogError(
            "ADMIN_TICKET deferred_refund_failed: OrderId={OrderId} OrderReturnId={OrderReturnId} " +
            "Intent={Intent} Amount={Amount} Reason={Reason}. " +
            "Use RetryDeferredRefundCommand to retry once the underlying issue is resolved.",
            request.OrderId,
            request.OrderReturnId,
            request.Intent,
            request.Amount,
            request.FailureReason);

        return Task.FromResult(UnitResult.Success<Error>());
    }
}
