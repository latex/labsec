using System.Collections.Concurrent;

namespace omama_cli.Services.CVE;

public record SlowSyncStats(int Scanned, int Existed, int Saved, int Errors);

public class CveSlowSyncService
{
    private readonly IReadOnlyList<NamedCveProvider> _providers;
    private readonly string _cacheDir;
    private readonly int _maxPerHourPerSource;
    private readonly int _globalConcurrency;

    public CveSlowSyncService(IReadOnlyList<NamedCveProvider> providers, string cacheDir, int maxPerHourPerSource = 500, int globalConcurrency = 4)
    {
        _providers = providers;
        _cacheDir = cacheDir;
        _maxPerHourPerSource = Math.Max(1, maxPerHourPerSource);
        _globalConcurrency = Math.Max(1, globalConcurrency);

        Directory.CreateDirectory(_cacheDir);
    }

    public async Task<SlowSyncStats> SyncLatestInterleavedAsync(int perSourceBatch = 10, bool force = false, CancellationToken ct = default)
    {
        // intervalo médio entre requisições para cumprir max/hora
        var minDelay = TimeSpan.FromSeconds(3600.0 / _maxPerHourPerSource);
        var lastCall = new ConcurrentDictionary<string, DateTimeOffset>();
        var throttler = new SemaphoreSlim(_globalConcurrency);
        var errors = 0;
        var existed = 0;
        var saved = 0;
        var scanned = 0;

        var cache = new HighPerformanceCache(_cacheDir);

        // round-robin simples: em cada ciclo, cada provedor busca um pequeno lote
        foreach (var named in _providers)
        {
            await EnforceSourceDelayAsync(named.Name, minDelay, lastCall, ct);
            var batch = await named.Provider.GetLatestCvesAsync(perSourceBatch);
            var list = batch.ToList();
            scanned += list.Count;

            var tasks = list.Select(cve => Task.Run(async () =>
            {
                await throttler.WaitAsync(ct);
                try
                {
                    var key = $"cve:id:{cve.Id}";
                    var exists = await cache.GetAsync<Models.CVE>(key) != null;
                    if (exists && !force)
                    {
                        Interlocked.Increment(ref existed);
                        return;
                    }

                    var detailed = await named.Provider.GetCveByIdAsync(cve.Id);
                    if (detailed != null)
                    {
                        await cache.SetAsync(key, detailed);
                        Interlocked.Increment(ref saved);
                    }
                }
                catch
                {
                    Interlocked.Increment(ref errors);
                }
                finally
                {
                    throttler.Release();
                }
            }, ct));

            await Task.WhenAll(tasks);
            lastCall[named.Name] = DateTimeOffset.UtcNow;
        }

        return new SlowSyncStats(scanned, existed, saved, errors);
    }

    private static async Task EnforceSourceDelayAsync(string source, TimeSpan minDelay, ConcurrentDictionary<string, DateTimeOffset> lastCall, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        if (lastCall.TryGetValue(source, out var last))
        {
            var elapsed = now - last;
            var wait = minDelay - elapsed;
            if (wait > TimeSpan.Zero)
            {
                await Task.Delay(wait, ct);
            }
        }
    }
}
