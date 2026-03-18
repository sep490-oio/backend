using Microsoft.EntityFrameworkCore;
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

    private readonly IDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMediaRelocationService _mediaRelocationService;
    private readonly IClock _clock;
    private readonly ILogger<PendingUploadRelocationJob> _logger;

    public PendingUploadRelocationJob(
        IDbContext dbContext,
        IUnitOfWork unitOfWork,
        IMediaRelocationService mediaRelocationService,
        IClock clock,
        ILogger<PendingUploadRelocationJob> logger)
    {
        _dbContext = dbContext;
        _unitOfWork = unitOfWork;
        _mediaRelocationService = mediaRelocationService;
        _clock = clock;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var nowUtc = _clock.UtcNow;
        var cancellationToken = context.CancellationToken;

        var candidates = await _dbContext.Set<MediaUpload>()
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
            await _mediaRelocationService.RelocateLinkedUploadAsync(upload, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Processed {Count} pending media relocation candidates.",
            candidates.Count);
    }
}
