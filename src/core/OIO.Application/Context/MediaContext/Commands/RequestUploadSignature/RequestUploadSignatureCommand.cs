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
    private readonly IRuntimeSettings _runtimeSettings;
    private readonly UploadContextRegistry _contextRegistry;

    public RequestUploadSignatureCommandHandler(
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

    public async Task<Result<UploadSignatureResponse, Error>> Handle(
        RequestUploadSignatureCommand request,
        CancellationToken cancellationToken)
    {
        var validationError = _contextRegistry.ValidateContext(request.Context);
        
        if (validationError.IsFailure)
            return validationError.Error;

        var contextConfig = _contextRegistry.Get(request.Context);
        
        var resourceType = UploadContextRegistry.ParseResourceType(contextConfig!.ResourceType);
        
        
        //  "items/pending/{userId}" (temporary folder)
        var folder = 
            $"{contextConfig.Folder}/pending/{_currentUser.UserId}";

        // Build upload public_id leaf. Cloudinary prefixes the folder separately.
        var uniqueSuffix = Guid.NewGuid().ToString("N")[..12];
        var prefix = resourceType.ToFilePrefix();
        var mediaName = $"{prefix}_{uniqueSuffix}";

        // Preserve .pdf extension on Cloudinary public_id for term_document so the
        // delivered URL stays a browser-openable PDF (res.cloudinary.com/.../raw/upload/.../xxx.pdf).
        if (_contextRegistry.IsTermContext(request.Context) && IsPdfFileName(request.FileName))
        {
            mediaName += ".pdf";
        }

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
            signatureExpirationMinutes: _runtimeSettings.Media.SignatureExpiration);

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

    private static bool IsPdfFileName(string? fileName)
        => !string.IsNullOrWhiteSpace(fileName)
           && fileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase);
}


