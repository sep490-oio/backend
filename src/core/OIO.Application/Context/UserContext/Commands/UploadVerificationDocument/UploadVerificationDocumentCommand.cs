using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Media;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.UserContext.DTOs;
using OIO.Application.Context.UserContext.Mappings;
using OIO.Application.Context.MediaContext.Services;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.Context.Shared.Entities;
using OIO.Domain.Context.Shared.Errors;
using OIO.Domain.Context.Shared.ValueObjects.Ids;
using OIO.Domain.Context.UserContext.Aggregates.Users;
using OIO.Domain.Context.UserContext.Enums;
using OIO.Domain.Context.UserContext.Errors;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.UserContext.Commands.UploadVerificationDocument;

public sealed record UploadVerificationDocumentCommand(
    Guid VerificationId,
    Guid MediaUploadId,
    string DocumentType) : ICommand<VerificationDocumentDto>, IHasValidate
{
    public ViolationsError Validate()
    {
        return UploadVerificationDocumentCommand.Check()
            .WithOwnerName("UploadVerificationDocument")
            .Field(VerificationId)
            .NotEmptyGuid()
            .Field(MediaUploadId)
            .NotEmptyGuid()
            .Field(DocumentType)
            .NotWhiteSpace()
            .InSet(VerificationDocumentType.All.Select(x => x.Id));
    }
}

internal sealed class UploadVerificationDocumentCommandHandler
    : ICommandHandler<UploadVerificationDocumentCommand, VerificationDocumentDto>
{
    private readonly IDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly ICurrentUser _currentUser;
    private readonly UploadContextRegistry _contextRegistry;
    private readonly IMediaRelocationService _mediaRelocationService;
    private readonly ILogger<UploadVerificationDocumentCommandHandler> _logger;

    public UploadVerificationDocumentCommandHandler(
        IDbContext dbContext,
        IUnitOfWork unitOfWork,
        IClock clock,
        ICurrentUser currentUser,
        UploadContextRegistry contextRegistry,
        IMediaRelocationService mediaRelocationService,
        ILogger<UploadVerificationDocumentCommandHandler> logger)
    {
        _dbContext = dbContext;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _currentUser = currentUser;
        _contextRegistry = contextRegistry;
        _mediaRelocationService = mediaRelocationService;
        _logger = logger;
    }

    public async Task<Result<VerificationDocumentDto, Error>> Handle(
        UploadVerificationDocumentCommand request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;
        var verificationId = IdentityVerificationId.From(request.VerificationId);

        var verification = await _dbContext.GetByIdAsync<IdentityVerification, IdentityVerificationId>(
            verificationId,
            q => q.Include(v => v.Documents),
            cancellationToken);

        if (verification is null || verification.UserId != userId)
            return UserErrors.Verification.NotFound(verificationId);

        var mediaUploadId = MediaUploadId.From(request.MediaUploadId);
        var upload = await _dbContext.GetByIdAsync<MediaUpload, MediaUploadId>(
            id: mediaUploadId,
            cancellationToken: cancellationToken);

        if (upload is null)
            return MediaErrors.NotFound(mediaUploadId);

        if (upload.UserId != userId)
        {
            _logger.LogWarning("User {UserId} attempted to use media upload {MediaUploadId} which they do not own.",
                userId, mediaUploadId);
            return MediaErrors.NotOwnedByUser(mediaUploadId);
        }

        if (!upload.IsConfirmed)
            return MediaErrors.NotConfirm;

        if (upload.IsLinked)
            return MediaErrors.AlreadyLinked;

        if (!_contextRegistry.IsVerificationContext(upload.Context))
        {
            _logger.LogWarning("Media upload {MediaUploadId} has invalid context {Context} for verification {VerificationId}.",
                mediaUploadId, upload.Context, verificationId);
            return MediaErrors.WrongContext(upload.Context, await _contextRegistry.GetAllContextAsync(cancellationToken));
        }

        var nowUtc = _clock.UtcNow;
        var docType = VerificationDocumentType.FromId(request.DocumentType).Value;
        var maxForType = await _contextRegistry.GetMaxForEntityMediaAsync("verification", upload.ResourceType, cancellationToken);

        var docResult = verification.AddDocument(docType, upload, maxForType, nowUtc);

        if (docResult.IsFailure)
            return docResult.Error;

        await _mediaRelocationService.RelocateLinkedUploadAsync(upload, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return docResult.Value.ToDto();
    }
}
