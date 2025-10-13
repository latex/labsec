using System.Buffers;
using System.Collections.Concurrent;
using System.Runtime;
using Microsoft.Extensions.Caching.Memory;
using System.Runtime.InteropServices;
using System.IO.Compression;
using System.IO;

namespace omama_cli.Services.CVE;

public class HighPerformanceCache : IDisposable
{
    private readonly string _cachePath;
    private readonly long _memoryLimit;
    private readonly MemoryCache _memoryCache;
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks;
    private bool _disposed;

    public HighPerformanceCache(string cachePath, long? memoryLimit = null)
    {
        _cachePath = cachePath;
        _memoryLimit = memoryLimit ?? GetRecommendedMemoryLimit();
        _memoryCache = new MemoryCache(new MemoryCacheOptions
        {
            SizeLimit = _memoryLimit,
            CompactionPercentage = 0.2, // Remove 20% quando atingir o limite
        });
        _locks = new ConcurrentDictionary<string, SemaphoreSlim>();

        Directory.CreateDirectory(_cachePath);
        ConfigureGCSettings();
    }

    public async Task ProcessAndCacheBulkAsync<T>(IEnumerable<T> items) where T : class
    {
        var batches = CreateOptimalBatches(items);
        var tasks = new List<Task>();

        foreach (var batch in batches)
        {
            tasks.Add(ProcessBatchAsync(batch));
        }

        await Task.WhenAll(tasks);
    }

    private IEnumerable<IEnumerable<T>> CreateOptimalBatches<T>(IEnumerable<T> items)
    {
        var availableMemory = GetAvailableMemory();
        var itemsList = items.ToList();
        var totalItems = itemsList.Count;
        var batchSize = Math.Max(1, (int)(totalItems * (availableMemory / (double)_memoryLimit)));

        return itemsList.Chunk(batchSize);
    }

    private async Task ProcessBatchAsync<T>(IEnumerable<T> batch) where T : class
    {
        foreach (var item in batch)
        {
            var key = GetCacheKey(item);
            await SetAsync(key, item, GetMemoryCacheEntryOptions());
        }
    }

    public async Task<T?> GetAsync<T>(string key) where T : class
    {
        // Tenta primeiro da memória
        if (_memoryCache.TryGetValue(key, out T? cachedValue))
        {
            return cachedValue;
        }

        // Se não encontrou, busca do disco
        var lockItem = _locks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
        await lockItem.WaitAsync();
        try
        {
            // Verifica novamente após obter o lock
            if (_memoryCache.TryGetValue(key, out cachedValue))
            {
                return cachedValue;
            }

            var filePath = GetFilePath(key);
            if (!File.Exists(filePath))
            {
                return null;
            }

            // Lê e descomprime os dados
            using (var fileStream = File.OpenRead(filePath))
            using (var gzipStream = new GZipStream(fileStream, CompressionMode.Decompress))
            using (var reader = new StreamReader(gzipStream))
            {
                var json = await reader.ReadToEndAsync();
                var item = System.Text.Json.JsonSerializer.Deserialize<T>(json);

                if (item != null)
                {
                    _memoryCache.Set(key, item, GetMemoryCacheEntryOptions());
                }

                return item;
            }
        }
        finally
        {
            lockItem.Release();
        }
    }

    public async Task SetAsync<T>(string key, T value, MemoryCacheEntryOptions? options = null)
    {
        var lockItem = _locks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
        await lockItem.WaitAsync();
        try
        {
            // Salva em memória
            _memoryCache.Set(key, value, options ?? GetMemoryCacheEntryOptions());

            // Salva em disco com compressão
            var filePath = GetFilePath(key);
            var json = System.Text.Json.JsonSerializer.Serialize(value);
            
            await using (var fileStream = File.Create(filePath))
            await using (var gzipStream = new GZipStream(fileStream, CompressionLevel.SmallestSize))
            await using (var writer = new StreamWriter(gzipStream))
            {
                await writer.WriteAsync(json);
            }
        }
        finally
        {
            lockItem.Release();
        }
    }

    public async Task<int> GetCachedItemCountAsync()
    {
        return Directory.GetFiles(_cachePath, "*.json").Length;
    }

    public async Task<IEnumerable<T>> GetAllCachedAsync<T>() where T : class
    {
    var files = Directory.GetFiles(_cachePath, "*.json.gz");
        var results = new List<T>();

        foreach (var file in files)
        {
            var key = Path.GetFileNameWithoutExtension(file);
            var item = await GetAsync<T>(key);
            if (item != null)
            {
                results.Add(item);
            }
        }

        return results;
    }

    private string GetFilePath(string key)
    {
    return Path.Combine(_cachePath, $"{key}.json.gz");
    }

    private static string GetCacheKey<T>(T item)
    {
        return item?.GetHashCode().ToString("X") ?? throw new ArgumentNullException(nameof(item));
    }

    private static MemoryCacheEntryOptions GetMemoryCacheEntryOptions()
    {
        return new MemoryCacheEntryOptions()
            .SetSize(1) // Cada entrada conta como 1 unidade
            .SetSlidingExpiration(TimeSpan.FromMinutes(30))
            .SetAbsoluteExpiration(TimeSpan.FromHours(24));
    }

    private static long GetAvailableMemory()
    {
        return GC.GetGCMemoryInfo().TotalAvailableMemoryBytes;
    }

    private static long GetRecommendedMemoryLimit()
    {
        var availableMemory = GetAvailableMemory();
        return (long)(availableMemory * 0.7); // Usa até 70% da memória disponível
    }

    private static void ConfigureGCSettings()
    {
        // Configura o GC para modo servidor se disponível
        if (GCSettings.IsServerGC)
        {
            // Configura para latência baixa
            GCSettings.LatencyMode = GCLatencyMode.SustainedLowLatency;
        }

        // Configura LOH para compactação
        GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _memoryCache.Dispose();
            foreach (var lockItem in _locks.Values)
            {
                lockItem.Dispose();
            }
            _disposed = true;
        }
    }

    ~HighPerformanceCache()
    {
        Dispose();
    }
}