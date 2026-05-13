using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Media;
using OIO.Domain.Context.CatalogContext.Aggregates.Categories;
using OIO.Domain.Context.CatalogContext.Aggregates.Items;
using OIO.Domain.Context.CatalogContext.ValueObjects.Ids;
using OIO.Domain.Context.ModerationContext.Aggregates.Disputes;
using OIO.Domain.Context.ModerationContext.ValueObjects.Ids;
using OIO.Domain.Context.OrderContext.Aggregates.SellerDirectShipments;
using OIO.Domain.Context.OrderContext.ValueObjects.Ids;
using OIO.Domain.Context.Shared.Entities;
using OIO.Domain.Context.Shared.Errors;
using OIO.Domain.Context.Shared.ValueObjects;
using OIO.Domain.Context.UserContext.Aggregates.Users;
using OIO.Domain.Context.UserContext.ValueObjects;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.Context.WarehouseContext.Aggregates.OutboundShipments;
using OIO.Domain.Context.WarehouseContext.Aggregates.WarehouseItems;
using OIO.Domain.Context.WarehouseContext.ValueObjects.Ids;
using OIO.Domain.Context.PaymentContext.Aggregates.Withdrawals;
using OIO.Domain.Context.PaymentContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Errors;
using CategoryId = OIO.Domain.Context.CatalogContext.ValueObjects.Ids.CategoryId;

namespace OIO.Application.Context.MediaContext.Services;

internal sealed class MediaRelocationService : IMediaRelocationService
{
    private const int MaxRetries = 3;

    private readonly IDbContext _dbContext;
    private readonly IClock _clock;
    private readonly UploadContextRegistry _contextRegistry;
    private readonly IMediaSignatureService _mediaSignatureService;
    private readonly ILogger<MediaRelocationService> _logger;

    public MediaRelocationService(
        IDbContext dbContext,
        IClock clock,
        UploadContextRegistry contextRegistry,
        IMediaSignatureService mediaSignatureService,
        ILogger<MediaRelocationService> logger)
    {
        _dbContext = dbContext;
        _clock = clock;
        _contextRegistry = contextRegistry;
        _mediaSignatureService = mediaSignatureService;
        _logger = logger;
    }

    public async Task RelocateLinkedUploadAsync(MediaUpload upload, CancellationToken cancellationToken = default)
    {
        if (!upload.RequiresRelocation())
            return;

        var nowUtc = _clock.UtcNow;

        if (upload.NextRelocationAttemptAt.HasValue && upload.NextRelocationAttemptAt > nowUtc)
            return;

        if (upload.EntityId is null)
        {
            ScheduleRetry(upload, "Linked upload does not have an entity id yet.", nowUtc);
            return;
        }

        var oldPublicId = upload.StorageRef.PublicId;

        try
        {
            var contextConfig = _contextRegistry.Get(upload.Context);
            if (contextConfig is null)
            {
                ScheduleRetry(upload, $"Upload context '{upload.Context}' is no longer configured.", nowUtc);
                return;
            }

            var leafPublicId = ExtractLeafPublicId(oldPublicId);
            if (string.IsNullOrWhiteSpace(leafPublicId))
            {
                ScheduleRetry(upload, $"Unable to determine leaf public id from '{oldPublicId}'.", nowUtc);
                return;
            }

            var targetFolder = await ResolveTargetFolderAsync(upload, contextConfig.Folder, cancellationToken);
            var targetPublicId = $"{targetFolder}/{leafPublicId}";

            if (string.Equals(oldPublicId, targetPublicId, StringComparison.Ordinal))
            {
                var existingStorage = StorageRef.Create(targetPublicId, targetFolder);
                if (existingStorage.IsFailure)
                {
                    ScheduleRetry(upload, existingStorage.Error.Message, nowUtc);
                    return;
                }

                var existingInfo = string.IsNullOrWhiteSpace(upload.Info.SecureUrl)
                    ? upload.Info
                    : upload.Info.WithSecureUrl(upload.Info.SecureUrl!);

                upload.MarkRelocationSucceeded(existingStorage.Value, existingInfo, nowUtc);
                return;
            }

            var resourceType = UploadContextRegistry.ParseResourceType(upload.ResourceType);
            var renameResult = await _mediaSignatureService.RenameResourceAsync(
                oldPublicId,
                targetPublicId,
                resourceType,
                cancellationToken);

            if (renameResult is null)
            {
                ScheduleRetry(upload, $"Cloudinary rename failed for '{oldPublicId}'.", nowUtc);
                return;
            }

            var storageRefResult = StorageRef.Create(renameResult.PublicId, renameResult.Folder);
            if (storageRefResult.IsFailure)
            {
                ScheduleRetry(upload, storageRefResult.Error.Message, nowUtc);
                return;
            }

            var secureUrl = ResolveSecureUrl(
                renameResult.SecureUrl,
                upload.Info.SecureUrl,
                oldPublicId,
                renameResult.PublicId);
            var refreshedInfo = string.IsNullOrWhiteSpace(secureUrl)
                ? upload.Info
                : upload.Info.WithSecureUrl(secureUrl);

            var refreshResult = await RefreshLinkedEntitySnapshotAsync(
                upload,
                oldPublicId,
                storageRefResult.Value,
                refreshedInfo,
                nowUtc,
                cancellationToken);

            if (refreshResult.IsFailure)
            {
                ScheduleRetry(upload, refreshResult.Error.Message, nowUtc);
                return;
            }

            var relocationResult = upload.MarkRelocationSucceeded(storageRefResult.Value, refreshedInfo, nowUtc);
            if (relocationResult.IsFailure)
            {
                ScheduleRetry(upload, relocationResult.Error.Message, nowUtc);
                return;
            }

            _logger.LogInformation(
                "Relocated media upload {MediaUploadId} from {FromPublicId} to {ToPublicId}.",
                upload.Id.Value,
                oldPublicId,
                renameResult.PublicId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Unexpected error while relocating media upload {MediaUploadId} from {OldPublicId}.",
                upload.Id.Value,
                oldPublicId);
            ScheduleRetry(upload, ex.Message, nowUtc);
        }
    }

    private async Task<UnitResult<Error>> RefreshLinkedEntitySnapshotAsync(
        MediaUpload upload,
        string oldPublicId,
        StorageRef storageRef,
        MediaInfo info,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        if (upload.EntityId is null)
            return Error.Conflict("Media.RelocationMissingEntityId", "Media upload is missing linked entity id.");

        if (_contextRegistry.IsItemContext(upload.Context))
        {
            var item = await FindItemAsync(upload.EntityId, cancellationToken);
            if (item is null)
                return Error.NotFound("Media.ItemNotFound", $"Item '{upload.EntityId}' was not found during media relocation.");

            return item.RefreshMediaSnapshot(oldPublicId, storageRef, info, nowUtc);
        }

        if (_contextRegistry.IsVerificationContext(upload.Context))
        {
            var verification = await FindVerificationAsync(upload.EntityId, cancellationToken);
            if (verification is null)
                return Error.NotFound("Media.VerificationNotFound", $"Verification '{upload.EntityId}' was not found during media relocation.");

            return verification.RefreshDocumentSnapshot(oldPublicId, storageRef, info, nowUtc);
        }

        if (_contextRegistry.IsUserAvatarContext(upload.Context))
        {
            var user = await FindUserAsync(upload.EntityId, cancellationToken);
            if (user is null)
                return Error.NotFound("Media.UserNotFound", $"User '{upload.EntityId}' was not found during media relocation.");

            if (string.IsNullOrWhiteSpace(info.SecureUrl))
                return MediaErrors.NotContainUrl;

            var avatarUrlResult = AvatarUrl.Create(info.SecureUrl);
            if (avatarUrlResult.IsFailure)
                return avatarUrlResult.Error;

            user.RefreshAvatarSnapshot(oldPublicId, avatarUrlResult.Value, nowUtc);
            return UnitResult.Success<Error>();
        }

        if (_contextRegistry.IsTermContext(upload.Context))
        {
            var document = await FindTermsDocumentAsync(upload.EntityId, cancellationToken);
            if (document is null)
                return Error.NotFound("Media.TermsDocumentNotFound", $"Terms document '{upload.EntityId}' was not found during media relocation.");

            document.RefreshMediaSnapshot(storageRef, info);
            return UnitResult.Success<Error>();
        }

        if (_contextRegistry.IsCategoryContext(upload.Context))
        {
            var category = await FindCategoryAsync(upload.EntityId, cancellationToken);
            if (category is null)
                return Error.NotFound("Media.CategoryNotFound", $"Category '{upload.EntityId}' was not found during media relocation.");

            if (!string.Equals(category.IconStorageRef?.PublicId, oldPublicId, StringComparison.Ordinal))
                return Error.NotFound("Media.CategoryIconNotFound", $"Category icon '{oldPublicId}' was not found during media relocation.");

            category.RefreshIconMedia(storageRef, info, nowUtc);
            return UnitResult.Success<Error>();
        }

        if (_contextRegistry.IsWarehouseInspectionContext(upload.Context))
        {
            var inspection = await FindWarehouseInspectionAsync(upload.EntityId, cancellationToken);
            if (inspection is null)
                return Error.NotFound("Media.WarehouseInspectionNotFound", $"Warehouse inspection '{upload.EntityId}' was not found during media relocation.");

            return inspection.RefreshEvidenceSnapshot(oldPublicId, storageRef, info, nowUtc);
        }

        if (_contextRegistry.IsShipmentContext(upload.Context))
        {
            // Shipment evidence stores a snapshot URL keyed by MediaUploadId
            // (not publicId). Dispatch to the right aggregate based on the
            // IdType captured at LinkToEntity time.
            if (string.Equals(upload.IdType, nameof(OutboundShipmentId), StringComparison.Ordinal))
            {
                var outbound = await FindOutboundShipmentAsync(upload.EntityId, cancellationToken);
                if (outbound is null)
                    return Error.NotFound(
                        "Media.OutboundShipmentNotFound",
                        $"Outbound shipment '{upload.EntityId}' was not found during media relocation.");

                return outbound.RefreshEvidenceSnapshot(upload.Id, info, nowUtc);
            }

            if (string.Equals(upload.IdType, nameof(SellerDirectShipmentId), StringComparison.Ordinal))
            {
                var direct = await FindSellerDirectShipmentAsync(upload.EntityId, cancellationToken);
                if (direct is null)
                    return Error.NotFound(
                        "Media.SellerDirectShipmentNotFound",
                        $"Seller direct shipment '{upload.EntityId}' was not found during media relocation.");

                return direct.RefreshEvidenceSnapshot(upload.Id, info, nowUtc);
            }

            return MediaErrors.UnsupportedShipmentEntityType(upload.IdType);
        }

        if (_contextRegistry.IsDisputeContext(upload.Context))
        {
            var attachment = await FindDisputeMessageAttachmentAsync(upload.EntityId, cancellationToken);
            if (attachment is null)
                return Error.NotFound("Media.DisputeAttachmentNotFound", $"Dispute attachment '{upload.EntityId}' was not found during media relocation.");

            attachment.RefreshMediaSnapshot(storageRef, info, nowUtc);
            return UnitResult.Success<Error>();
        }

        if (_contextRegistry.IsWithdrawalContext(upload.Context))
        {
            var withdrawal = await FindWithdrawalRequestAsync(upload.EntityId, cancellationToken);
            if (withdrawal is null)
                return Error.NotFound("Media.WithdrawalNotFound", $"Withdrawal request '{upload.EntityId}' was not found during media relocation.");

            var oldUrl = upload.Info.SecureUrl;
            var newUrl = info.SecureUrl;
            if (!string.IsNullOrWhiteSpace(oldUrl) && !string.IsNullOrWhiteSpace(newUrl))
            {
                var refreshResult = withdrawal.RefreshTransferProofUrl(oldUrl, newUrl);
                if (refreshResult.IsFailure)
                    return refreshResult.Error;
            }
            return UnitResult.Success<Error>();
        }

        return Error.Validation("context", "Media.UnsupportedContext", $"Media relocation does not support context '{upload.Context}'.");
    }

    private void ScheduleRetry(MediaUpload upload, string errorMessage, DateTime nowUtc)
    {
        DateTime? nextRetryAt = upload.RelocationAttemptCount >= MaxRetries
            ? null
            : nowUtc.AddMinutes(upload.RelocationAttemptCount switch
            {
                0 => 1,
                1 => 5,
                _ => 15
            });

        upload.MarkRelocationRetry(errorMessage, nextRetryAt);

        if (nextRetryAt.HasValue)
        {
            _logger.LogWarning(
                "Media upload {MediaUploadId} relocation failed. Retrying at {NextRetryAt}. Error: {Error}",
                upload.Id.Value,
                nextRetryAt.Value,
                errorMessage);
            return;
        }

        _logger.LogWarning(
            "Media upload {MediaUploadId} relocation exhausted retries. Error: {Error}",
            upload.Id.Value,
            errorMessage);
    }

    private async Task<Item?> FindItemAsync(string entityId, CancellationToken cancellationToken)
    {
        var itemId = ItemId.Parse(entityId);
        var local = _dbContext.Set<Item>().Local.FirstOrDefault(x => x.Id == itemId);
        if (local is not null)
            return local;

        return await _dbContext.GetByIdAsync<Item, ItemId>(
            itemId,
            query => query.Include(x => x.Media),
            cancellationToken);
    }

    private async Task<IdentityVerification?> FindVerificationAsync(string entityId, CancellationToken cancellationToken)
    {
        var verificationId = IdentityVerificationId.Parse(entityId);
        var local = _dbContext.Set<IdentityVerification>().Local.FirstOrDefault(x => x.Id == verificationId);
        if (local is not null)
            return local;

        return await _dbContext.GetByIdAsync<IdentityVerification, IdentityVerificationId>(
            verificationId,
            query => query.Include(x => x.Documents),
            cancellationToken);
    }

    private async Task<User?> FindUserAsync(string entityId, CancellationToken cancellationToken)
    {
        var userId = UserId.Parse(entityId);
        var local = _dbContext.Set<User>().Local.FirstOrDefault(x => x.Id == userId);
        if (local is not null && local.Profile is not null)
            return local;

        return await _dbContext.GetByIdAsync<User, UserId>(
            userId,
            query => query.Include(x => x.Profile),
            cancellationToken);
    }

    private async Task<TermsDocument?> FindTermsDocumentAsync(string entityId, CancellationToken cancellationToken)
    {
        var documentId = TermsDocumentId.Parse(entityId);
        var local = _dbContext.Set<TermsDocument>().Local.FirstOrDefault(x => x.Id == documentId);
        if (local is not null)
            return local;

        return await _dbContext.GetByIdAsync<TermsDocument, TermsDocumentId>(
            documentId,
            cancellationToken: cancellationToken);
    }

    private async Task<Category?> FindCategoryAsync(string entityId, CancellationToken cancellationToken)
    {
        var categoryId = CategoryId.Parse(entityId);
        var local = _dbContext.Set<Category>().Local.FirstOrDefault(x => x.Id == categoryId);
        if (local is not null)
            return local;

        return await _dbContext.GetByIdAsync<Category, CategoryId>(
            categoryId,
            cancellationToken: cancellationToken);
    }

    private async Task<WarehouseInspection?> FindWarehouseInspectionAsync(string entityId, CancellationToken cancellationToken)
    {
        var inspectionId = WarehouseInspectionId.Parse(entityId);
        var local = _dbContext.Set<WarehouseInspection>().Local.FirstOrDefault(x => x.Id == inspectionId);
        if (local is not null)
            return local;

        return await _dbContext.GetByIdAsync<WarehouseInspection, WarehouseInspectionId>(
            inspectionId,
            cancellationToken: cancellationToken);
    }

    private async Task<OutboundShipment?> FindOutboundShipmentAsync(string entityId, CancellationToken cancellationToken)
    {
        var shipmentId = OutboundShipmentId.Parse(entityId);
        var local = _dbContext.Set<OutboundShipment>().Local.FirstOrDefault(x => x.Id == shipmentId);
        if (local is not null)
            return local;

        return await _dbContext.GetByIdAsync<OutboundShipment, OutboundShipmentId>(
            shipmentId,
            query => query.Include(x => x.Evidence),
            cancellationToken);
    }

    private async Task<SellerDirectShipment?> FindSellerDirectShipmentAsync(string entityId, CancellationToken cancellationToken)
    {
        var shipmentId = SellerDirectShipmentId.Parse(entityId);
        var local = _dbContext.Set<SellerDirectShipment>().Local.FirstOrDefault(x => x.Id == shipmentId);
        if (local is not null)
            return local;

        return await _dbContext.GetByIdAsync<SellerDirectShipment, SellerDirectShipmentId>(
            shipmentId,
            query => query.Include(x => x.Evidence),
            cancellationToken);
    }

    private async Task<DisputeMessageAttachment?> FindDisputeMessageAttachmentAsync(string entityId, CancellationToken cancellationToken)
    {
        var attachmentId = DisputeMessageAttachmentId.Parse(entityId);
        var local = _dbContext.Set<DisputeMessageAttachment>().Local.FirstOrDefault(x => x.Id == attachmentId);
        if (local is not null)
            return local;

        return await _dbContext.GetByIdAsync<DisputeMessageAttachment, DisputeMessageAttachmentId>(
            attachmentId,
            cancellationToken: cancellationToken);
    }

    private async Task<WithdrawalRequest?> FindWithdrawalRequestAsync(string entityId, CancellationToken cancellationToken)
    {
        var withdrawalId = WithdrawalRequestId.Parse(entityId);
        var local = _dbContext.Set<WithdrawalRequest>().Local.FirstOrDefault(x => x.Id == withdrawalId);
        if (local is not null)
            return local;

        return await _dbContext.GetByIdAsync<WithdrawalRequest, WithdrawalRequestId>(
            withdrawalId,
            cancellationToken: cancellationToken);
    }

    private async Task<string> ResolveTargetFolderAsync(
        MediaUpload upload,
        string baseFolder,
        CancellationToken cancellationToken)
    {
        if (!_contextRegistry.IsDisputeContext(upload.Context) || upload.EntityId is null)
            return $"{baseFolder}/{upload.EntityId}";

        var attachment = await FindDisputeMessageAttachmentAsync(upload.EntityId, cancellationToken);
        if (attachment is null)
            return $"{baseFolder}/{upload.EntityId}";

        return $"{baseFolder}/{attachment.DisputeId.Value}/messages/{attachment.DisputeMessageId.Value}";
    }

    private static string ExtractLeafPublicId(string publicId)
        => publicId.Split('/', StringSplitOptions.RemoveEmptyEntries).LastOrDefault() ?? string.Empty;

    private static string? ResolveSecureUrl(
        string? renamedSecureUrl,
        string? currentSecureUrl,
        string oldPublicId,
        string newPublicId)
    {
        if (!string.IsNullOrWhiteSpace(renamedSecureUrl))
            return renamedSecureUrl;

        if (string.IsNullOrWhiteSpace(currentSecureUrl))
            return currentSecureUrl;

        return currentSecureUrl.Replace(oldPublicId, newPublicId, StringComparison.Ordinal);
    }
}

