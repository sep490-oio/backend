using System.Globalization;
using Microsoft.AspNetCore.SignalR;
using OIO.Api.Services;
using OIO.Application.Context.AuctionContext.DTOs;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Api.Hubs;

public sealed class AuctionBidIdempotencyHubFilter : IHubFilter
{
    private readonly IdempotencyCacheService _cacheService;

    public AuctionBidIdempotencyHubFilter(IdempotencyCacheService cacheService)
    {
        _cacheService = cacheService;
    }

    public async ValueTask<object?> InvokeMethodAsync(
        HubInvocationContext invocationContext,
        Func<HubInvocationContext, ValueTask<object?>> next)
    {
        if (!string.Equals(invocationContext.HubMethodName, nameof(AuctionHub.PlaceBid), StringComparison.Ordinal))
            return await next(invocationContext);

        if (invocationContext.HubMethodArguments.Count < 4)
            return HubCommandResult<PlaceBidResultDto>.FromError(CreateBidIdempotencyError(IdempotencyFailureKind.MissingKey));

        var auctionId = (Guid)invocationContext.HubMethodArguments[0]!;
        var amount = (decimal)invocationContext.HubMethodArguments[1]!;
        var currency = (string)invocationContext.HubMethodArguments[2]!;
        var idempotencyKey = invocationContext.HubMethodArguments[3] as string;
        var currentUser = invocationContext.ServiceProvider.GetRequiredService<ICurrentUser>();
        var fingerprint = string.Join(
            "|",
            amount.ToString("0.########", CultureInfo.InvariantCulture),
            currency.Trim().ToUpperInvariant());

        var cached = await _cacheService.ExecuteAsync(
            idempotencyKey,
            $"bid:idempotency:{currentUser.UserId.Value}:{auctionId}",
            fingerprint,
            async () =>
            {
                var raw = await next(invocationContext);
                return raw as HubCommandResult<PlaceBidResultDto>
                       ?? HubCommandResult<PlaceBidResultDto>.FromError(
                           Error.Unexpected(
                               "Idempotency.HubResultInvalid",
                               "Hub returned an unsupported response for idempotency."));
            },
            failureKind => HubCommandResult<PlaceBidResultDto>.FromError(CreateBidIdempotencyError(failureKind)),
            [$"bid-idempotency:{currentUser.UserId.Value}:{auctionId}"],
            invocationContext.Context.ConnectionAborted);

        return cached;
    }

    private static Error CreateBidIdempotencyError(IdempotencyFailureKind failureKind) => failureKind switch
    {
        IdempotencyFailureKind.MissingKey => Error.Validation(
            "idempotencyKey",
            "Idempotency.Required",
            "Idempotency-Key is required for this operation."),
        IdempotencyFailureKind.PayloadMismatch => Error.Conflict(
            "Bid.IdempotencyPayloadMismatch",
            "Idempotency key was already used with a different bid payload."),
        _ => Error.Unexpected(
            "Idempotency.CacheCorrupted",
            "Cached idempotent response is invalid.")
    };
}
