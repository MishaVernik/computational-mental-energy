using CmeSim.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CmeSim.Api.Services;

/// <summary>
/// On API startup, idempotently ensures the User / Headband / 4 Electrode twins
/// and one Activity twin per ActionDefinition exist in Azure Digital Twins.
/// Runs once and exits the loop. Failures are logged but never crash the process
/// (the rest of the API runs without ADT).
/// </summary>
public sealed class DigitalTwinBootstrapper : BackgroundService
{
    private readonly IDigitalTwinSyncService _sync;
    private readonly AzureDigitalTwinsOptions _opts;
    private readonly ILogger<DigitalTwinBootstrapper> _logger;
    private readonly IServiceProvider _serviceProvider;

    public DigitalTwinBootstrapper(
        IDigitalTwinSyncService sync,
        IOptions<AzureDigitalTwinsOptions> opts,
        ILogger<DigitalTwinBootstrapper> logger,
        IServiceProvider serviceProvider)
    {
        _sync = sync;
        _opts = opts.Value;
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_opts.IsEnabled)
        {
            _logger.LogInformation("DigitalTwinBootstrapper skipped (Azure DT disabled).");
            return;
        }

        await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);

        try
        {
            await _sync.EnsureBaseTwinsAsync(stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "DigitalTwinBootstrapper failed; continuing without ADT mirror.");
        }

        // Activity catalogue: one shared twin per active ActionDefinition row.
        // Only meaningful if the concrete DigitalTwinSyncService is wired (no-op fallback is ignored).
        if (_sync is DigitalTwinSyncService syncImpl)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<CmeSimDbContext>();
                var defs = await db.ActionDefinitions
                    .Where(a => a.IsActive)
                    .ToListAsync(stoppingToken);

                foreach (var def in defs)
                {
                    await syncImpl.UpsertActivityTwinAsync(def.Slug, def.Name, def.DefaultDifficulty, def.Icon, def.IsSystem);
                }

                _logger.LogInformation("Activity catalogue ensured: {Count} twins.", defs.Count);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Activity-catalogue bootstrap failed; continuing.");
            }
        }
    }
}
