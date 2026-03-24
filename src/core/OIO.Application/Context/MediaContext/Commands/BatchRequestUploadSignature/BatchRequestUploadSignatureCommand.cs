using System.Collections;
using CSharpFunctionalExtensions;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Media;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.MediaContext.Commands.RequestUploadSignature;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.Context.Shared.Entities;
using OIO.Domain.Context.Shared.Errors;
using OIO.Domain.Context.Shared.ValueObjects;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.MediaContext.Commands.BatchRequestUploadSignature;

public sealed record BatchUploadSignatureItem(
    string Context,
    string FileName);

public sealed record BatchRequestUploadSignatureCommand(
    List<BatchUploadSignatureItem> Items) : ICommand<List<UploadSignatureResponse>>, IHasValidate
{
    public ViolationsError Validate()
    {
        var check = BatchRequestUploadSignatureCommand.Check()
            .WithOwnerName("BatchRequestUploadSignature")
            .Field((IEnumerable)Items)
            .CountMin(1)
            .CountMax(10);

        for (var i = 0; i < Items.Count; i++)
        {
            var item = Items[i];
            check
                .Field(item.Context, propertyName: $"Items[{i}].Context")
                .NotWhiteSpace()
                .Field(item.FileName, propertyName: $"Items[{i}].FileName")
                .NotWhiteSpace();
        }

        return check;
    }
}

internal sealed class BatchRequestUploadSignatureCommandHandler
    : ICommandHandler<BatchRequestUploadSignatureCommand, List<UploadSignatureResponse>>
{
    private readonly IMediaSignatureService _signatureService;
    private readonly IDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IClock _clock;
    private readonly IRuntimeSettings _runtimeSettings;
    private readonly UploadContextRegistry _contextRegistry;

    public BatchRequestUploadSignatureCommandHandler(
        IMediaSignatureService signatureService,
        IDbContext dbContext,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IClock clock,
        IRuntimeSettings runtimeSettings,
        UploadContextRegistry contextRegistry)
    {
        _signatureService = signatureService;
        _dbContext = dbContext;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _clock = clock;
        _runtimeSettings = runtimeSettings;
        _contextRegistry = contextRegistry;
    }

    public async Task<Result<List<UploadSignatureResponse>, Error>> Handle(
        BatchRequestUploadSignatureCommand request,
        CancellationToken cancellationToken)
    {
        var responses = new List<UploadSignatureResponse>(request.Items.Count);

        foreach (var item in request.Items)
        {
            var validationError = _contextRegistry.ValidateContext(item.Context);

            if (validationError.IsFailure)
                return validationError.Error;

            var contextConfig = _contextRegistry.Get(item.Context);

            var resourceType = UploadContextRegistry.ParseResourceType(contextConfig!.ResourceType);

            //  "items/pending/{userId}" (temporary folder)
            var folder =
                $"{contextConfig.Folder}/pending/{_currentUser.UserId}";

            // Build upload public_id leaf. Cloudinary prefixes the folder separately.
            var uniqueSuffix = Guid.NewGuid().ToString("N")[..12];
            var prefix = resourceType.ToFilePrefix();
            var mediaName = $"{prefix}_{uniqueSuffix}";

            // Generate Cloudinary signature
            var signatureResult = _signatureService.GenerateSignature(
                resourceType: resourceType,
                mediaName: mediaName,
                folder: folder,
                eager: contextConfig.Eager,
                allowedFormats: contextConfig.AllowedFormats);

            var nowUtc = _clock.UtcNow;

            var mediaInfo = MediaInfo.Create(
                fileName: item.FileName);

            var (_, isFailure, storage, error) = StorageRef.Create(
                publicId: signatureResult.StoragePublicId,
                folder: signatureResult.Folder);

            if (isFailure)
            {
                return error;
            }

            // Track pending upload in DB
            var mediaUpload = MediaUpload.Create(
                userId: _currentUser.UserId,
                context: item.Context,
                resourceType: contextConfig.ResourceType,
                entityId: null,
                idType: null,
                mediaInfo: mediaInfo,
                storageRef: storage,
                nowUtc: nowUtc,
                signatureExpirationMinutes: _runtimeSettings.Media.SignatureExpiration);

            _dbContext.Insert(mediaUpload);

            responses.Add(new UploadSignatureResponse(
                MediaUploadId: mediaUpload.Id.Value,
                UploadUrl: signatureResult.UploadUrl,
                Signature: signatureResult.Signature,
                Timestamp: signatureResult.Timestamp,
                ApiKey: signatureResult.ApiKey,
                CloudName: signatureResult.CloudName,
                PublicId: signatureResult.UploadPublicId,
                StoragePublicId: signatureResult.StoragePublicId,
                Folder: signatureResult.Folder,
                Eager: signatureResult.Eager,
                ResourceType: signatureResult.ResourceType,
                MaxFileSize: contextConfig.MaxFileSizeBytes,
                AllowedFormats: contextConfig.AllowedFormats));
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return responses;
    }
}
