namespace omama_cli.Services.CVE;

public class CveProviderCacheDecorator : ICveDataProvider, IDisposable
{
    private readonly ICveDataProvider _inner;
    private readonly HighPerformanceCache _cache;
    private readonly ParallelOptions _parallelOptions;
    private const string CachePrefix = "cve";
    private bool _disposed;

    ~CveProviderCacheDecorator()
    {
        Dispose(false);
    }

    public CveProviderCacheDecorator(ICveDataProvider inner, TimeSpan cacheTimeout)
    {
        _inner = inner;
        var cachePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "omama-cli",
            "cache");
        _cache = new HighPerformanceCache(cachePath);
        _parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = Environment.ProcessorCount * 2
        };
    }

    public async Task<Models.CVE?> GetCveByIdAsync(string cveId)
    {
        var cacheKey = $"{CachePrefix}:id:{cveId}";
        var cached = await _cache.GetAsync<Models.CVE>(cacheKey);
        
        if (cached != null)
            return cached;

        var result = await _inner.GetCveByIdAsync(cveId);
        if (result != null)
            await _cache.SetAsync(cacheKey, result);

        return result;
    }

    public async Task<IEnumerable<Models.CVE>> SearchCvesAsync(string keyword)
    {
        var cacheKey = $"{CachePrefix}:search:{keyword}";
        var cached = await _cache.GetAsync<List<Models.CVE>>(cacheKey);
        
        if (cached != null)
            return cached;

        var results = (await _inner.SearchCvesAsync(keyword)).ToList();
        
        // Processa e enriquece os dados em paralelo
        await Parallel.ForEachAsync(
            results,
            _parallelOptions,
            async (cve, ct) =>
            {
                var detailedCve = await GetCveByIdAsync(cve.Id);
                if (detailedCve != null)
                {
                    cve.References = detailedCve.References;
                    cve.Score = detailedCve.Score;
                    cve.Severity = detailedCve.Severity;
                }
            });

        await _cache.SetAsync(cacheKey, results);
        return results;
    }

    public async Task<IEnumerable<Models.CVE>> GetLatestCvesAsync(int limit = 10)
    {
        var cacheKey = $"{CachePrefix}:latest:{limit}";
        var cached = await _cache.GetAsync<List<Models.CVE>>(cacheKey);
        
        if (cached != null)
            return cached;

        var results = (await _inner.GetLatestCvesAsync(limit)).ToList();
        
        // Processa e enriquece os dados em paralelo
        await Parallel.ForEachAsync(
            results,
            _parallelOptions,
            async (cve, ct) =>
            {
                var detailedCve = await GetCveByIdAsync(cve.Id);
                if (detailedCve != null)
                {
                    cve.References = detailedCve.References;
                    cve.Score = detailedCve.Score;
                    cve.Severity = detailedCve.Severity;
                }
            });

        await _cache.SetAsync(cacheKey, results);
        return results;
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _cache?.Dispose();
            }
            _disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}