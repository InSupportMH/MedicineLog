using MedicineLog.Data;
using Microsoft.EntityFrameworkCore;

namespace MedicineLog.Services;

public sealed class LogCleanupService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IPhotoStoreService _photoStore;
    private readonly ILogger<LogCleanupService> _log;

    public LogCleanupService(
        IServiceScopeFactory scopeFactory,
        IPhotoStoreService photoStore,
        ILogger<LogCleanupService> log)
    {
        _scopeFactory = scopeFactory;
        _photoStore = photoStore;
        _log = log;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var timer = new PeriodicTimer(TimeSpan.FromMinutes(30)); // choose what you like

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await CleanupOnce(stoppingToken);
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Cleanup job failed");
            }
        }
    }

    private async Task CleanupOnce(CancellationToken ct)
    {
        var cutoff = DateTimeOffset.UtcNow.AddHours(-48);

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Pull only what you need (IDs + photo paths)
        var expired = await db.MedicineLogEntries
            .Where(e => e.CreatedAt < cutoff)
            .Select(e => new
            {
                Entry = e,
                PhotoPaths = e.Items.Select(i => i.PhotoPath).ToList()
            })
            .ToListAsync(ct);

        if (expired.Count == 0) return;

        foreach (var e in expired)
        {
            foreach (var path in e.PhotoPaths)
                await _photoStore.DeleteAsync(path, ct);

            db.MedicineLogEntries.Remove(e.Entry);
        }

        await db.SaveChangesAsync(ct);

        _log.LogInformation("Cleanup removed {Count} entries older than {Cutoff}", expired.Count, cutoff);
    }
}
