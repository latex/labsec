using System.Security.Cryptography;
using System.Text.Json;

namespace omama_cli.Services.CVE;

public class CveHashCache
{
    private readonly string _cachePath;
    private readonly TimeSpan _maxAge;
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true
    };

    public CveHashCache(string cachePath, TimeSpan? maxAge = null)
    {
        _cachePath = cachePath;
        _maxAge = maxAge ?? TimeSpan.FromDays(5);
        
        if (!Directory.Exists(_cachePath))
        {
            Directory.CreateDirectory(_cachePath);
        }
    }

    public async Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory) where T : class
    {
        var cacheFile = GetCacheFilePath(key);
        var cacheEntry = await LoadFromCacheAsync<CacheEntry<T>>(cacheFile);

        if (cacheEntry != null && !IsCacheExpired(cacheEntry))
        {
            return cacheEntry.Data;
        }

        var data = await factory();
        await SaveToCacheAsync(cacheFile, new CacheEntry<T>
        {
            Data = data,
            CreatedAt = DateTime.UtcNow,
            Hash = ComputeDataHash(data)
        });

        return data;
    }

    public async Task<IEnumerable<T>> GetOrCreateBulkAsync<T>(
        string keyPrefix,
        IEnumerable<string> identifiers,
        Func<string, Task<T>> factory) where T : class
    {
        var results = new List<T>();
        var tasks = new List<Task<T>>();

        foreach (var id in identifiers)
        {
            var key = $"{keyPrefix}:{id}";
            tasks.Add(GetOrCreateAsync(key, () => factory(id)));
        }

        return await Task.WhenAll(tasks);
    }

    private async Task<TCacheEntry?> LoadFromCacheAsync<TCacheEntry>(string filePath) where TCacheEntry : class
    {
        if (!File.Exists(filePath))
        {
            return null;
        }

        try
        {
            var json = await File.ReadAllTextAsync(filePath);
            return JsonSerializer.Deserialize<TCacheEntry>(json, _jsonOptions);
        }
        catch (Exception ex)
        {
            // Se houver erro na leitura do cache, retorna null para forçar recriação
            Console.Error.WriteLine($"Erro ao ler cache: {ex.Message}");
            return null;
        }
    }

    private async Task SaveToCacheAsync<T>(string filePath, T data)
    {
        try
        {
            var json = JsonSerializer.Serialize(data, _jsonOptions);
            await File.WriteAllTextAsync(filePath, json);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Erro ao salvar cache: {ex.Message}");
            // Continua a execução mesmo com erro no cache
        }
    }

    private bool IsCacheExpired<T>(CacheEntry<T> entry)
    {
        return DateTime.UtcNow - entry.CreatedAt > _maxAge;
    }

    private string GetCacheFilePath(string key)
    {
        return Path.Combine(_cachePath, $"{ComputeHash(key)}.json");
    }

    private static string ComputeHash(string input)
    {
        using var sha256 = SHA256.Create();
        var bytes = System.Text.Encoding.UTF8.GetBytes(input);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToHexString(hash);
    }

    private static string ComputeDataHash<T>(T data)
    {
        var json = JsonSerializer.Serialize(data);
        using var sha256 = SHA256.Create();
        var bytes = System.Text.Encoding.UTF8.GetBytes(json);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToHexString(hash);
    }
}

public class CacheEntry<T>
{
    public T Data { get; set; } = default!;
    public DateTime CreatedAt { get; set; }
    public string Hash { get; set; } = string.Empty;
}