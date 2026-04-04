using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Context.MediaContext.Services;
using OIO.Domain.Context.Shared.Entities;
using Quartz;

namespace OIO.Infrastructure.Scheduling.Jobs;

[DisallowConcurrentExecution]
public sealed class PendingUploadRelocationJob : IJob
{
    private const int BatchSize = 50;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IClock _clock;
    private readonly ILogger<PendingUploadRelocationJob> _logger;

    public PendingUploadRelocationJob(
        IServiceScopeFactory scopeFactory,
        IClock clock,
        ILogger<PendingUploadRelocationJob> logger)
    {
        _scopeFactory = scopeFactory;
        _clock = clock;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IDbContext>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var mediaRelocationService = scope.ServiceProvider.GetRequiredService<IMediaRelocationService>();

        var nowUtc = _clock.UtcNow;
        var cancellationToken = context.CancellationToken;

        var candidates = await dbContext.Set<MediaUpload>()
            .Where(x => x.IsLinked
                        && x.RelocatedAt == null
                        && x.StorageRef.Folder.Contains("/pending/")
                        && (x.NextRelocationAttemptAt == null || x.NextRelocationAttemptAt <= nowUtc))
            .OrderBy(x => x.NextRelocationAttemptAt ?? x.LinkedAt ?? x.CreatedAt)
            .Take(BatchSize)
            .ToListAsync(cancellationToken);

        if (candidates.Count == 0)
            return;

        foreach (var upload in candidates)
            await mediaRelocationService.RelocateLinkedUploadAsync(upload, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Processed {Count} pending media relocation candidates.",
            candidates.Count);
    }
}
