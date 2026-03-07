using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.Context.Shared.Entities;
using OIO.Domain.Context.Shared.Errors;
using OIO.Domain.Context.Shared.ValueObjects.Ids;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.MediaContext.Commands.ConfirmUpload;

public sealed record ConfirmUploadCommand(
    Guid MediaUploadId,
    string PublicId,
    string SecureUrl,
    long Bytes,
    string Format,
    int? Width,
    int? Height,
    double? DurationSeconds) : ICommand<ConfirmUploadResponse>, IHasValidate
{
    public ViolationsError Validate()
    {
        return ConfirmUploadCommand.Check()
            .WithOwnerName("ConfirmUpload")
            .Field(MediaUploadId)
            .NotEmptyGuid()
            .Field(PublicId)
            .NotWhiteSpace()
            .Field(SecureUrl)
            .NotWhiteSpace()
            .Format(x => Uri.TryCreate(x, UriKind.Absolute, out _))
            .Field(Bytes)
            .GreaterThan(0)
            .Field(Format)
            .NotWhiteSpace();

    }
}

public sealed record ConfirmUploadResponse(
    Guid MediaUploadId,
    string SecureUrl,
    string PublicId,
    string ResourceType);

internal sealed class ConfirmUploadCommandHandler
    : ICommandHandler<ConfirmUploadCommand, ConfirmUploadResponse>
{
    private readonly IDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IAppConfigs _appConfigs;
    private readonly IClock _clock;
    private readonly ILogger<ConfirmUploadCommandHandler> _logger;

    public ConfirmUploadCommandHandler(
        IDbContext dbContext,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IAppConfigs appConfigs,
        IClock clock,
        ILogger<ConfirmUploadCommandHandler> logger)
    {
        _dbContext = dbContext;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _appConfigs = appConfigs;
        _clock = clock;
        _logger = logger;
    }

    public async Task<Result<ConfirmUploadResponse, Error>> Handle(
        ConfirmUploadCommand request,
        CancellationToken cancellationToken)
    {
        var mediaUploadId = MediaUploadId.From(request.MediaUploadId);
        
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

        if (mediaUpload.PublicId != request.PublicId)
        {
            _logger.LogWarning("PublicId mismatch for media upload {MediaUploadId}. Expected: {ExpectedPublicId}, Actual: {ActualPublicId}",
                mediaUploadId, mediaUpload.PublicId, request.PublicId);
            return MediaErrors.PublicIdMismatch;
        }
        
        var result = mediaUpload.Confirm(
            secureUrl: request.SecureUrl,
            bytes: request.Bytes,
            format: request.Format,
            width: request.Width,
            height: request.Height,
            orphanExpirationMinutes: await _appConfigs.Media.GetOrphanExpirationMinutesAsync(cancellationToken),
            durationSeconds: request.DurationSeconds,
            nowUtc: _clock.UtcNow);

        if (result.IsFailure)
        {
            return result.Error;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new ConfirmUploadResponse(
            MediaUploadId: mediaUpload.Id.Value,
            SecureUrl: request.SecureUrl,
            PublicId: request.PublicId,
            ResourceType: mediaUpload.ResourceType);
    }
}