using System.Collections.Concurrent;
using System.Diagnostics;

namespace omama_cli.Services.CVE;

public record SlowSyncStats(int Scanned, int Existed, int Saved, int Errors, TimeSpan Duration, Dictionary<string, object> Metrics);

public class CveSlowSyncService
{
    private readonly IReadOnlyList<NamedCveProvider> _providers;
    private readonly string _cacheDir;
    private readonly int _maxPerHourPerSource;
    private readonly int _globalConcurrency;
    private static bool _loggingEnabled = Environment.GetEnvironmentVariable("OMAMA_STAT") == "homol";

    public CveSlowSyncService(IReadOnlyList<NamedCveProvider> providers, string cacheDir, int maxPerHourPerSource = 500, int globalConcurrency = 4)
    {
        _providers = providers;
        _cacheDir = cacheDir;
        _maxPerHourPerSource = Math.Max(1, maxPerHourPerSource);
        _globalConcurrency = Math.Max(1, globalConcurrency);

        Directory.CreateDirectory(_cacheDir);
        Log($"SlowSyncService inicializado: {providers.Count} providers, maxPerHour={maxPerHourPerSource}, concurrency={globalConcurrency}");
    }

    private static void Log(string message)
    {
        if (_loggingEnabled)
            Console.WriteLine($"[SYNC] {DateTime.Now:HH:mm:ss.fff} {message}");
    }

    public async Task<SlowSyncStats> SyncLatestInterleavedAsync(int perSourceBatch = 10, bool force = false, CancellationToken ct = default)
    {
        var overallStopwatch = Stopwatch.StartNew();
        Log($"Iniciando sync interleaved: batch={perSourceBatch}, force={force}, providers={_providers.Count}");
        
        // intervalo médio entre requisições para cumprir max/hora
        var minDelay = TimeSpan.FromSeconds(3600.0 / _maxPerHourPerSource);
        var lastCall = new ConcurrentDictionary<string, DateTimeOffset>();
        var throttler = new SemaphoreSlim(_globalConcurrency);
        var errors = 0;
        var existed = 0;
        var saved = 0;
        var scanned = 0;

        var sourceMetrics = new ConcurrentDictionary<string, Dictionary<string, object>>();
        var cache = new HighPerformanceCache(_cacheDir);

        Log($"Rate limit configurado: {minDelay.TotalSeconds:F2}s entre requisições");

        // round-robin simples: em cada ciclo, cada provedor busca um pequeno lote
        foreach (var named in _providers)
        {
            var sourceStopwatch = Stopwatch.StartNew();
            Log($"Processando provider: {named.Name}");
            
            await EnforceSourceDelayAsync(named.Name, minDelay, lastCall, ct);
            
            Log($"Chamando GetLatestCvesAsync({perSourceBatch}) para {named.Name}");
            var batch = await named.Provider.GetLatestCvesAsync(perSourceBatch);
            var list = batch.ToList();
            scanned += list.Count;
            
            Log($"Provider {named.Name} retornou {list.Count} CVEs para processamento");

            var sourceErrors = 0;
            var sourceExisted = 0;
            var sourceSaved = 0;

            var tasks = list.Select(cve => Task.Run(async () =>
            {
                await throttler.WaitAsync(ct);
                var cveStopwatch = Stopwatch.StartNew();
                try
                {
                    var key = $"cve:id:{cve.Id}";
                    Log($"Processando CVE {cve.Id} de {named.Name}");
                    
                    var exists = await cache.GetAsync<Models.CVE>(key) != null;
                    if (exists && !force)
                    {
                        Log($"CVE {cve.Id} já existe no cache, pulando");
                        Interlocked.Increment(ref existed);
                        Interlocked.Increment(ref sourceExisted);
                        return;
                    }

                    Log($"Buscando detalhes do CVE {cve.Id}");
                    var detailed = await named.Provider.GetCveByIdAsync(cve.Id);
                    if (detailed != null)
                    {
                        // Anota a fonte para contagem posterior no cache
                        detailed.Source = named.Name;
                        await cache.SetAsync(key, detailed);
                        Log($"CVE {cve.Id} salvo no cache com sucesso");
                        Interlocked.Increment(ref saved);
                        Interlocked.Increment(ref sourceSaved);
                    }
                    else
                    {
                        Log($"AVISO: Não foi possível obter detalhes do CVE {cve.Id}");
                    }
                }
                catch (Exception ex)
                {
                    Log($"ERRO processando CVE {cve.Id}: {ex.Message}");
                    Interlocked.Increment(ref errors);
                    Interlocked.Increment(ref sourceErrors);
                }
                finally
                {
                    cveStopwatch.Stop();
                    throttler.Release();
                    Log($"CVE {cve.Id} processado em {cveStopwatch.ElapsedMilliseconds}ms");
                }
            }, ct));

            await Task.WhenAll(tasks);
            sourceStopwatch.Stop();
            
            Log($"Provider {named.Name} concluído: {list.Count} verificados, {sourceExisted} existiam, {sourceSaved} salvos, {sourceErrors} erros em {sourceStopwatch.ElapsedMilliseconds}ms");
            lastCall[named.Name] = DateTimeOffset.UtcNow;
        }

        overallStopwatch.Stop();
        var metrics = new Dictionary<string, object>
        {
            ["ProvidersProcessed"] = _providers.Count,
            ["RateLimitDelaySeconds"] = minDelay.TotalSeconds,
            ["GlobalConcurrency"] = _globalConcurrency
        };

        Log($"Sync completo em {overallStopwatch.ElapsedMilliseconds}ms: {scanned} verificados, {existed} existiam, {saved} salvos, {errors} erros");
        return new SlowSyncStats(scanned, existed, saved, errors, overallStopwatch.Elapsed, metrics);
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
