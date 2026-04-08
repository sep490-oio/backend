using CSharpFunctionalExtensions;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.OrderContext.Services;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.OrderContext.Commands.ConfirmOrderReceipt;

public sealed record ConfirmOrderReceiptCommand(Guid OrderId) : ICommand;

internal sealed class ConfirmOrderReceiptCommandHandler(
    IClock clock,
    IOrderReceiptService orderReceiptService)
    : ICommandHandler<ConfirmOrderReceiptCommand>
{
    public Task<UnitResult<Error>> Handle(
        ConfirmOrderReceiptCommand request,
        CancellationToken cancellationToken)
    {
        return orderReceiptService.ConfirmAsync(
            request.OrderId,
            systemInvoked: false,
            clock.UtcNow,
            cancellationToken);
    }
}
