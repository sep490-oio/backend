using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OIO.Application.Abstractions.Address;
using Quartz;

namespace OIO.Infrastructure.Scheduling.Jobs;

/// <summary>
/// Periodically synchronizes address data (Provinces, Districts, Wards) from GHN API.
/// This ensures local DistrictID and WardCode values are up to date.
/// </summary>
[DisallowConcurrentExecution]
public sealed class SyncGhnAddressJob : IJob
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SyncGhnAddressJob> _logger;

    public SyncGhnAddressJob(
        IServiceScopeFactory scopeFactory,
        ILogger<SyncGhnAddressJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        _logger.LogInformation("Starting automated GHN address synchronization...");

        using var scope = _scopeFactory.CreateScope();
        var addressService = scope.ServiceProvider.GetRequiredService<IGhnAddressService>();

        try
        {
            var result = await addressService.SyncAllAsync(context.CancellationToken);

            _logger.LogInformation(
                "GHN address sync completed successfully. provinces={Provinces}, districts={Districts}, wards={Wards}",
                result.ProvinceCount,
                result.DistrictCount,
                result.WardCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred during GHN address synchronization.");
        }
    }
}
