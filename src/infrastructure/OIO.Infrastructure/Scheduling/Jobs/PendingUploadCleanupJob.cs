using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Media;
using OIO.Domain.Context.Shared.Entities;
using Quartz;

namespace OIO.Infrastructure.Scheduling.Jobs;

[DisallowConcurrentExecution]
public sealed class PendingUploadCleanupJob : IJob
{
    private readonly IDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMediaSignatureService _mediaSignatureService;
    private readonly IClock _clock;
    private readonly IRuntimeSettings _runtimeSettings;
    private readonly ILogger<PendingUploadCleanupJob> _logger;

    public PendingUploadCleanupJob(
        IDbContext dbContext,
        IUnitOfWork unitOfWork,
        IMediaSignatureService mediaSignatureService,
        IClock clock,
        IRuntimeSettings runtimeSettings,
        ILogger<PendingUploadCleanupJob> logger)
    {
        _dbContext = dbContext;
        _unitOfWork = unitOfWork;
        _mediaSignatureService = mediaSignatureService;
        _clock = clock;
        _runtimeSettings = runtimeSettings;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var now = _clock.UtcNow;
        var cancellationToken = context.CancellationToken;
        var orphanThreshold = now.Add(-_runtimeSettings.Media.OrphanExpiration);
        var linkedRetentionThreshold = now.Add(-_runtimeSettings.Media.LinkedRecordRetention);

        var toDelete = new List<MediaUpload>();

        // Case 1: Signature expired, never confirmed
        var expiredUnconfirmed = await _dbContext.Set<MediaUpload>()
            .Where(p => !p.IsConfirmed && p.ExpiresAt < now)
            .ToListAsync(cancellationToken);

        if (expiredUnconfirmed.Count > 0)
        {
            _logger.LogInformation(
                "Found {Count} expired unconfirmed uploads.", expiredUnconfirmed.Count);
            toDelete.AddRange(expiredUnconfirmed);
        }

        // Case 2: Confirmed but never linked (orphan)
        var orphans = await _dbContext.Set<MediaUpload>()
            .Where(p => p.IsConfirmed &&
                        !p.IsLinked &&
                        p.ConfirmedAt < orphanThreshold)
            .ToListAsync(cancellationToken);

        if (orphans.Count > 0)
        {
            _logger.LogInformation(
                "Found {Count} orphan confirmed uploads.", orphans.Count);
            toDelete.AddRange(orphans);
        }

        // Case 3: Old linked records (audit trail cleanup)
        var oldLinked = await _dbContext.Set<MediaUpload>()
            .Where(p => p.IsLinked &&
                        p.LinkedAt < linkedRetentionThreshold &&
                        !p.StorageRef.Folder.Contains("/pending/"))
            .ToListAsync(cancellationToken);

        if (oldLinked.Count > 0)
        {
            _logger.LogInformation(
                "Cleaning {Count} old linked upload records.", oldLinked.Count);
            _dbContext.Set<MediaUpload>().RemoveRange(oldLinked);
        }

        // Delete from Cloudinary + DB
        // Batch delete from Cloudinary (grouped by resource type)
        if (toDelete.Count > 0)
        {
            var confirmedToDelete = toDelete
                .Where(p => p.IsConfirmed)
                .Select(p => (p.StorageRef.PublicId!, ParseResourceType(p.ResourceType)))
                .ToList();

            if (confirmedToDelete.Count > 0)
            {
                var deleted = await _mediaSignatureService.DeleteResourcesAsync(
                    confirmedToDelete, cancellationToken);

                _logger.LogInformation(
                    "Batch deleted {Deleted}/{Total} Cloudinary resources.",
                    deleted, confirmedToDelete.Count);
            }

            _dbContext.Set<MediaUpload>().RemoveRange(toDelete);

            _logger.LogInformation(
                "Cleaned up {Expired} expired + {Orphan} orphan uploads.",
                expiredUnconfirmed.Count, orphans.Count);
        }

        if (toDelete.Count > 0 || oldLinked.Count > 0)
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }

    private static MediaResourceType ParseResourceType(string resourceType) =>
        resourceType switch
        {
            "image" => MediaResourceType.Image,
            "video" => MediaResourceType.Video,
            "raw" => MediaResourceType.Raw,
            _ => MediaResourceType.Image
        };
}

