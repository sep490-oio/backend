using CSharpFunctionalExtensions;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Media;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.Context.Shared.Entities;
using OIO.Domain.Context.Shared.Errors;
using OIO.Domain.Context.Shared.ValueObjects;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.MediaContext.Commands.RequestUploadSignature;

public sealed record RequestUploadSignatureCommand(
    string Context,
    string FileName) : ICommand<UploadSignatureResponse>, IHasValidate
{
    public ViolationsError Validate()
    {
        return RequestUploadSignatureCommand.Check()
            .WithOwnerName("RequestUploadSignature")
            .Field(Context)
            .NotWhiteSpace()
            .Field(FileName)
            .NotWhiteSpace();
    }
}

public sealed record UploadSignatureResponse(
    Guid MediaUploadId,
    string UploadUrl,
    string Signature,
    long Timestamp,
    string ApiKey,
    string CloudName,
    string PublicId,
    string StoragePublicId,
    string Folder,
    string? Eager,
    string ResourceType,
    long MaxFileSize,
    string[] AllowedFormats);
    
internal sealed class RequestUploadSignatureCommandHandler
    : ICommandHandler<RequestUploadSignatureCommand, UploadSignatureResponse>
{
    private readonly IMediaSignatureService _signatureService;
    private readonly IDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IClock _clock;
    private readonly IAppConfigs _appConfigs;
    private readonly UploadContextRegistry _contextRegistry;

    public RequestUploadSignatureCommandHandler(
        IMediaSignatureService signatureService,
        IDbContext dbContext,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IClock clock,
        IAppConfigs appConfigs,
        UploadContextRegistry contextRegistry)
    
    {
        _signatureService = signatureService;
        _dbContext = dbContext;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _clock = clock;
        _appConfigs = appConfigs;
        _contextRegistry = contextRegistry;
    }

    public async Task<Result<UploadSignatureResponse, Error>> Handle(
        RequestUploadSignatureCommand request,
        CancellationToken cancellationToken)
    {
        var validationError = await _contextRegistry.ValidateContextAsync(request.Context, cancellationToken);
        
        if (validationError.IsFailure)
            return validationError.Error;

        var contextConfig = await _contextRegistry.GetAsync(request.Context, cancellationToken);
        
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
            fileName: request.FileName);
        
        var ( _, isFailure, storage, error) = StorageRef.Create(
            publicId: signatureResult.StoragePublicId,
            folder: signatureResult.Folder);

        if (isFailure)
        {
            return error;
        }

        // Track pending upload in DB
        var mediaUpload = MediaUpload.Create(
            userId: _currentUser.UserId,
            context: request.Context,
            resourceType: contextConfig.ResourceType,
            entityId: null,
            idType: null,
            mediaInfo: mediaInfo,
            storageRef: storage,
            nowUtc: nowUtc,
            signatureExpirationMinutes: await _appConfigs.Media.GetSignatureExpirationMinutesAsync(cancellationToken));

        _dbContext.Insert(mediaUpload);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new UploadSignatureResponse(
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
            AllowedFormats: contextConfig.AllowedFormats);
    }
}
