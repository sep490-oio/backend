using CSharpFunctionalExtensions;
using OIO.Api.Filters.Http;
using OIO.Api.Services;
using OIO.Application.Context.AuctionContext.DTOs;
using OIO.Application.Context.MediaContext.Commands.ConfirmUpload;
using OIO.Application.Context.MediaContext.Commands.RequestUploadSignature;
using OIO.Application.Context.ModerationContext.DTOs;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Api.Filters;

public enum IdempotencySuccessResultKind
{
    Ok,
    Created
}

public sealed record IdempotencyEndpointPolicy<TPayload>(
    Func<EndpointFilterInvocationContext, ICurrentUser, string> CacheKeyPrefixFactory,
    Func<EndpointFilterInvocationContext, string> FingerprintFactory,
    Func<EndpointFilterInvocationContext, ICurrentUser, string[]> TagsFactory,
    Func<IdempotencyFailureKind, Error> ErrorFactory,
    IdempotencySuccessResultKind SuccessResultKind);

public sealed class IdempotencyFilter<TPayload> : IEndpointFilter
{
    private readonly IdempotencyEndpointPolicy<TPayload> _policy;

    public IdempotencyFilter(IdempotencyEndpointPolicy<TPayload> policy)
    {
        _policy = policy;
    }

    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        var services = context.HttpContext.RequestServices;
        var cacheService = services.GetRequiredService<IdempotencyCacheService>();
        var currentUser = services.GetRequiredService<ICurrentUser>();
        var idempotencyKey = context.HttpContext.Request.Headers["Idempotency-Key"].FirstOrDefault();
        var fingerprint = _policy.FingerprintFactory(context);

        var cached = await cacheService.ExecuteAsync(
            idempotencyKey,
            _policy.CacheKeyPrefixFactory(context, currentUser),
            fingerprint,
            async () =>
            {
                var raw = await next(context);
                return ToCachedResult(raw);
            },
            failureKind => CachedHttpCommandResult<TPayload>.FromError(_policy.ErrorFactory(failureKind)),
            _policy.TagsFactory(context, currentUser),
            context.HttpContext.RequestAborted);

        return _policy.SuccessResultKind switch
        {
            IdempotencySuccessResultKind.Ok => cached.ToOkResult(),
            IdempotencySuccessResultKind.Created => cached.ToCreatedResult(),
            _ => cached.ToProblemResult()
        };
    }

    private static CachedHttpCommandResult<TPayload> ToCachedResult(object? raw)
    {
        if (raw is Result<TPayload, Error> result)
            return CachedHttpCommandResult<TPayload>.From(result);

        return CachedHttpCommandResult<TPayload>.FromError(
            Error.Unexpected(
                "Idempotency.EndpointResultInvalid",
                "Endpoint returned an unsupported response for idempotency."));
    }
}

public static class IdempotencyHttpPolicies
{
    public static IdempotencyEndpointPolicy<UploadSignatureResponse> RequestUploadSignature() =>
        new(
            CacheKeyPrefixFactory: (_, currentUser) =>
                $"media:idempotency:request-signature:{currentUser.UserId.Value}",
            FingerprintFactory: context =>
            {
                var request = context.Arguments
                    .OfType<Endpoints.MediaContext.Media.RequestUploadSignatureEndpoint.Request>()
                    .FirstOrDefault();

                var requestContext = request?.Context?.Trim() ?? string.Empty;
                var fileName = request?.FileName?.Trim() ?? string.Empty;

                return $"{requestContext}|{fileName}";
            },
            TagsFactory: (_, currentUser) => [$"media-idempotency:{currentUser.UserId.Value}"],
            ErrorFactory: CreateMediaError,
            SuccessResultKind: IdempotencySuccessResultKind.Ok);

    public static IdempotencyEndpointPolicy<ConfirmUploadResponse> ConfirmUpload() =>
        new(
            CacheKeyPrefixFactory: (context, currentUser) =>
            {
                var request = context.Arguments
                    .OfType<Endpoints.MediaContext.Media.ConfirmUploadEndpoint.Request>()
                    .FirstOrDefault();

                var mediaUploadId = request?.MediaUploadId ?? Guid.Empty;
                return $"media:idempotency:confirm:{currentUser.UserId.Value}:{mediaUploadId}";
            },
            FingerprintFactory: context =>
            {
                var request = context.Arguments
                    .OfType<Endpoints.MediaContext.Media.ConfirmUploadEndpoint.Request>()
                    .FirstOrDefault();

                return string.Join(
                    "|",
                    (request?.MediaUploadId ?? Guid.Empty).ToString(),
                    request?.PublicId?.Trim() ?? string.Empty,
                    request?.SecureUrl?.Trim() ?? string.Empty,
                    (request?.Bytes ?? 0L).ToString(System.Globalization.CultureInfo.InvariantCulture),
                    request?.Format?.Trim() ?? string.Empty,
                    request?.FileName?.Trim() ?? string.Empty,
                    request?.Width?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty,
                    request?.Height?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty,
                    request?.DurationSeconds?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty);
            },
            TagsFactory: (_, currentUser) => [$"media-idempotency:{currentUser.UserId.Value}"],
            ErrorFactory: CreateMediaError,
            SuccessResultKind: IdempotencySuccessResultKind.Created);

    public static IdempotencyEndpointPolicy<PlaceBidResultDto> PlaceBid() =>
        new(
            CacheKeyPrefixFactory: (context, currentUser) =>
            {
                var auctionId = context.Arguments.OfType<Guid>().First();
                return $"bid:idempotency:{currentUser.UserId.Value}:{auctionId}";
            },
            FingerprintFactory: context =>
            {
                var request = context.Arguments
                    .OfType<Endpoints.AuctionContext.Auctions.PlaceBidEndpoint.Request>()
                    .FirstOrDefault();

                return string.Join(
                    "|",
                    (request?.Amount ?? 0m).ToString("0.########", System.Globalization.CultureInfo.InvariantCulture),
                    request?.Currency?.Trim().ToUpperInvariant() ?? string.Empty);
            },
            TagsFactory: (context, currentUser) =>
            {
                var auctionId = context.Arguments.OfType<Guid>().First();
                return [$"bid-idempotency:{currentUser.UserId.Value}:{auctionId}"];
            },
            ErrorFactory: CreateBidError,
            SuccessResultKind: IdempotencySuccessResultKind.Created);

    public static IdempotencyEndpointPolicy<BuyNowCheckoutDto> BuyNow() =>
        new(
            CacheKeyPrefixFactory: (context, currentUser) =>
            {
                var auctionId = context.Arguments.OfType<Guid>().First();
                return $"buy-now:idempotency:{currentUser.UserId.Value}:{auctionId}";
            },
            FingerprintFactory: _ => "buy-now",
            TagsFactory: (context, currentUser) =>
            {
                var auctionId = context.Arguments.OfType<Guid>().First();
                return [$"buy-now-idempotency:{currentUser.UserId.Value}:{auctionId}"];
            },
            ErrorFactory: CreateBuyNowError,
            SuccessResultKind: IdempotencySuccessResultKind.Created);

    public static IdempotencyEndpointPolicy<DisputeMessageDto> SendDisputeMessage() =>
        new(
            CacheKeyPrefixFactory: (context, currentUser) =>
            {
                var disputeId = context.Arguments.OfType<Guid>().First();
                return $"dispute:idempotency:message:{currentUser.UserId.Value}:{disputeId}";
            },
            FingerprintFactory: context =>
            {
                var request = context.Arguments
                    .OfType<Endpoints.ModerationContext.Disputes.SendDisputeMessageEndpoint.Request>()
                    .FirstOrDefault();

                var trimmedMessage = request?.Message?.Trim() ?? string.Empty;
                var attachmentFingerprint = string.Join(
                    ",",
                    (request?.MediaUploadIds ?? [])
                        .OrderBy(x => x)
                        .Select(x => x.ToString("N")));

                return $"{trimmedMessage}|{attachmentFingerprint}|{request?.IsInternal ?? false}";
            },
            TagsFactory: (context, currentUser) =>
            {
                var disputeId = context.Arguments.OfType<Guid>().First();
                return [$"dispute-idempotency:{currentUser.UserId.Value}:{disputeId}"];
            },
            ErrorFactory: CreateDisputeError,
            SuccessResultKind: IdempotencySuccessResultKind.Created);

    private static Error CreateMediaError(IdempotencyFailureKind failureKind) => failureKind switch
    {
        IdempotencyFailureKind.MissingKey => Error.Validation(
            "idempotencyKey",
            "Idempotency.Required",
            "Idempotency-Key header is required for this operation."),
        IdempotencyFailureKind.PayloadMismatch => Error.Conflict(
            "Media.IdempotencyPayloadMismatch",
            "Idempotency key was already used with a different upload payload."),
        _ => Error.Unexpected(
            "Idempotency.CacheCorrupted",
            "Cached idempotent response is invalid.")
    };

    private static Error CreateBidError(IdempotencyFailureKind failureKind) => failureKind switch
    {
        IdempotencyFailureKind.MissingKey => Error.Validation(
            "idempotencyKey",
            "Idempotency.Required",
            "Idempotency-Key header is required for this operation."),
        IdempotencyFailureKind.PayloadMismatch => Error.Conflict(
            "Bid.IdempotencyPayloadMismatch",
            "Idempotency key was already used with a different bid payload."),
        _ => Error.Unexpected(
            "Idempotency.CacheCorrupted",
            "Cached idempotent response is invalid.")
    };

    private static Error CreateDisputeError(IdempotencyFailureKind failureKind) => failureKind switch
    {
        IdempotencyFailureKind.MissingKey => Error.Validation(
            "idempotencyKey",
            "Idempotency.Required",
            "Idempotency-Key header is required for this operation."),
        IdempotencyFailureKind.PayloadMismatch => Error.Conflict(
            "Dispute.IdempotencyPayloadMismatch",
            "Idempotency key was already used with a different dispute message payload."),
        _ => Error.Unexpected(
            "Idempotency.CacheCorrupted",
            "Cached idempotent response is invalid.")
    };

    private static Error CreateBuyNowError(IdempotencyFailureKind failureKind) => failureKind switch
    {
        IdempotencyFailureKind.MissingKey => Error.Validation(
            "idempotencyKey",
            "Idempotency.Required",
            "Idempotency-Key header is required for this operation."),
        IdempotencyFailureKind.PayloadMismatch => Error.Conflict(
            "BuyNow.IdempotencyPayloadMismatch",
            "Idempotency key was already used with a different buy-now payload."),
        _ => Error.Unexpected(
            "Idempotency.CacheCorrupted",
            "Cached idempotent response is invalid.")
    };
}
