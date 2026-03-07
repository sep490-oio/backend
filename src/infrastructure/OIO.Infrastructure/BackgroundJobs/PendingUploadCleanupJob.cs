using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Media;
using OIO.Domain.Context.Shared.Entities;

namespace OIO.Infrastructure.BackgroundJobs;

/// <summary>
/// Periodically cleans up expired pending uploads:
/// 1. Delete the resource from Cloudinary
/// 2. Remove the PendingUpload record from DB
/// </summary>
public sealed class PendingUploadCleanupJob : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PendingUploadCleanupJob> _logger;
    private TimeSpan _interval = TimeSpan.FromMinutes(15);

    public PendingUploadCleanupJob(
        IServiceScopeFactory scopeFactory,
        ILogger<PendingUploadCleanupJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
       
        _logger.LogInformation(
            "Pending upload cleanup job started. Interval: {Interval}", _interval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CleanupExpiredUploadsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in pending upload cleanup job.");
            }

            await Task.Delay(_interval, stoppingToken);
        }
    }

    private async Task CleanupExpiredUploadsAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IDbContext>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var signatureService = scope.ServiceProvider.GetRequiredService<IMediaSignatureService>();
        var clock = scope.ServiceProvider.GetRequiredService<IClock>();
        var appConfigs = scope.ServiceProvider.GetRequiredService<IAppConfigs>();
        
        _interval = await appConfigs.Media.GetCleanupIntervalMinutesAsync(cancellationToken: cancellationToken);
        
        var now = clock.UtcNow;
        
        var orphanThreshold = now.Add(- await appConfigs.Media.GetOrphanExpirationMinutesAsync(cancellationToken));
        var linkedRetentionThreshold = now.Add(- await appConfigs.Media.GetLinkedRecordRetentionDaysAsync(cancellationToken));

        var toDelete = new List<MediaUpload>();

        // Case 1: Signature expired, never confirmed
        var expiredUnconfirmed = await dbContext.Set<MediaUpload>()
            .Where(p => !p.IsConfirmed && p.ExpiresAt < now)
            .ToListAsync(cancellationToken);

        if (expiredUnconfirmed.Count > 0)
        {
            _logger.LogInformation(
                "Found {Count} expired unconfirmed uploads.", expiredUnconfirmed.Count);
            toDelete.AddRange(expiredUnconfirmed);
        }

        // Case 2: Confirmed but never linked (orphan)
        var orphans = await dbContext.Set<MediaUpload>()
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
        var oldLinked = await dbContext.Set<MediaUpload>()
            .Where(p => p.IsLinked &&
                        p.LinkedAt < linkedRetentionThreshold)
            .ToListAsync(cancellationToken);

        if (oldLinked.Count > 0)
        {
            _logger.LogInformation(
                "Cleaning {Count} old linked upload records.", oldLinked.Count);
            dbContext.Set<MediaUpload>().RemoveRange(oldLinked);
        }

        // Delete from Cloudinary + DB
        // Batch delete from Cloudinary (grouped by resource type)
        if (toDelete.Count > 0)
        {
            var confirmedToDelete = toDelete
                .Where(p => p.IsConfirmed)
                .Select(p => (p.PublicId, ParseResourceType(p.ResourceType)))
                .ToList();

            if (confirmedToDelete.Count > 0)
            {
                var deleted = await signatureService.DeleteResourcesAsync(
                    confirmedToDelete, cancellationToken);

                _logger.LogInformation(
                    "Batch deleted {Deleted}/{Total} Cloudinary resources.",
                    deleted, confirmedToDelete.Count);
            }

            dbContext.Set<MediaUpload>().RemoveRange(toDelete);

            _logger.LogInformation(
                "Cleaned up {Expired} expired + {Orphan} orphan uploads.",
                expiredUnconfirmed.Count, orphans.Count);
        }

        if (toDelete.Count > 0 || oldLinked.Count > 0)
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
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