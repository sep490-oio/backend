using CSharpFunctionalExtensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.OrderContext.Services;

/// <summary>
/// Buyer-confirms-receipt side-effect pipeline. Extracted from
/// <c>ConfirmOrderReceiptCommandHandler</c> so the overdue self-ship scan job
/// can invoke the same flow with <c>systemInvoked: true</c> (skipping the
/// <c>ICurrentUser</c> ownership check).
/// </summary>
public interface IOrderReceiptService
{
    Task<UnitResult<Error>> ConfirmAsync(
        Guid orderId,
        bool systemInvoked,
        DateTime nowUtc,
        CancellationToken cancellationToken);
}
