using System.Collections;
using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.MediaContext.Commands.ConfirmUpload;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.Context.Shared.Entities;
using OIO.Domain.Context.Shared.Errors;
using OIO.Domain.Context.Shared.ValueObjects;
using OIO.Domain.Context.Shared.ValueObjects.Ids;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.MediaContext.Commands.BatchConfirmUpload;

public sealed record BatchConfirmUploadItem(
    Guid MediaUploadId,
    string PublicId,
    string SecureUrl,
    long Bytes,
    string Format,
    string? FileName,
    int? Width,
    int? Height,
    double? DurationSeconds);

public sealed record BatchConfirmUploadCommand(
    List<BatchConfirmUploadItem> Items) : ICommand<List<ConfirmUploadResponse>>, IHasValidate
{
    public ViolationsError Validate()
    {
        var check = BatchConfirmUploadCommand.Check()
            .WithOwnerName("BatchConfirmUpload")
            .Field((IEnumerable)Items)
            .CountMin(1)
            .CountMax(10);

        for (var i = 0; i < Items.Count; i++)
        {
            var item = Items[i];
            check
                .Field(item.MediaUploadId, propertyName: $"Items[{i}].MediaUploadId")
                .NotEmptyGuid()
                .Field(item.PublicId, propertyName: $"Items[{i}].PublicId")
                .NotWhiteSpace()
                .Field(item.SecureUrl, propertyName: $"Items[{i}].SecureUrl")
                .NotWhiteSpace()
                .Format(x => Uri.TryCreate(x, UriKind.Absolute, out _))
                .Field(item.Bytes, propertyName: $"Items[{i}].Bytes")
                .GreaterThan(0)
                .Field(item.Format, propertyName: $"Items[{i}].Format")
                .NotWhiteSpace();
        }

        return check;
    }
}

internal sealed class BatchConfirmUploadCommandHandler
    : ICommandHandler<BatchConfirmUploadCommand, List<ConfirmUploadResponse>>
{
    private readonly IDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IRuntimeSettings _runtimeSettings;
    private readonly IClock _clock;
    private readonly ILogger<BatchConfirmUploadCommandHandler> _logger;

    public BatchConfirmUploadCommandHandler(
        IDbContext dbContext,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IRuntimeSettings runtimeSettings,
        IClock clock,
        ILogger<BatchConfirmUploadCommandHandler> logger)
    {
        _dbContext = dbContext;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _runtimeSettings = runtimeSettings;
        _clock = clock;
        _logger = logger;
    }

    public async Task<Result<List<ConfirmUploadResponse>, Error>> Handle(
        BatchConfirmUploadCommand request,
        CancellationToken cancellationToken)
    {
        var responses = new List<ConfirmUploadResponse>(request.Items.Count);

        foreach (var item in request.Items)
        {
            var mediaUploadId = MediaUploadId.From(item.MediaUploadId);

            var mediaUpload = await _dbContext.Set<MediaUpload>()
                .FirstOrDefaultAsync(p => p.Id == mediaUploadId, cancellationToken);

            if (mediaUpload is null)
                return MediaErrors.NotFound(mediaUploadId);

            if (mediaUpload.UserId != _currentUser.UserId)
            {
                _logger.LogWarning("User {UserId} attempted to confirm media upload {MediaUploadId} that belongs to another user {OwnerId}",
                    _currentUser.UserId, mediaUploadId, mediaUpload.UserId);
                return MediaErrors.NotOwnedByUser(mediaUploadId);
            }

            if (!MatchesPublicId(mediaUpload.StorageRef.PublicId, item.PublicId))
            {
                _logger.LogWarning("PublicId mismatch for media upload {MediaUploadId}. Expected: {ExpectedPublicId}, Actual: {ActualPublicId}",
                    mediaUploadId, mediaUpload.StorageRef.PublicId, item.PublicId);
                return MediaErrors.PublicIdMismatch;
            }

            var mediaInfo = MediaInfo.Create(
                secureUrl: item.SecureUrl,
                fileName: item.FileName,
                bytes: item.Bytes,
                format: item.Format,
                width: item.Width,
                height: item.Height,
                durationSeconds: item.DurationSeconds);

            var result = mediaUpload.Confirm(
                mediaInfo: mediaInfo,
                orphanExpirationMinutes: _runtimeSettings.Media.OrphanExpiration,
                nowUtc: _clock.UtcNow);

            if (result.IsFailure)
            {
                return result.Error;
            }

            responses.Add(new ConfirmUploadResponse(
                MediaUploadId: mediaUpload.Id.Value,
                SecureUrl: item.SecureUrl,
                PublicId: item.PublicId,
                ResourceType: mediaUpload.ResourceType));
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return responses;
    }

    private static bool MatchesPublicId(string expectedPublicId, string actualPublicId)
    {
        if (string.Equals(expectedPublicId, actualPublicId, StringComparison.Ordinal))
            return true;

        var expectedLeaf = expectedPublicId.Split('/', StringSplitOptions.RemoveEmptyEntries).LastOrDefault();
        var actualLeaf = actualPublicId.Split('/', StringSplitOptions.RemoveEmptyEntries).LastOrDefault();

        return !string.IsNullOrWhiteSpace(expectedLeaf) &&
               !string.IsNullOrWhiteSpace(actualLeaf) &&
               string.Equals(expectedLeaf, actualLeaf, StringComparison.Ordinal);
    }
}
