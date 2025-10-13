using System.Collections.Concurrent;

namespace omama_cli.Services.CVE;

public record CveSyncResult(
    string Mode,
    int Scanned,
    int Existed,
    int Saved,
    IReadOnlyList<string> Errors);

public class CveSyncService
{
    private readonly ICveDataProvider _provider;
    private readonly string _cacheDir;
    private readonly int _maxParallelism;

    public CveSyncService(ICveDataProvider provider, string cacheDir, int maxParallelism)
    {
        _provider = provider;
        _cacheDir = cacheDir;
        _maxParallelism = Math.Max(1, maxParallelism);

        Directory.CreateDirectory(_cacheDir);
    }

    private string CacheFileForId(string id) => Path.Combine(_cacheDir, $"cve:id:{id}.json.gz");

    public async Task<CveSyncResult> SyncAsync(string? keyword = null, int latest = 50, bool force = false, CancellationToken ct = default)
    {
        var errors = new ConcurrentBag<string>();
        int scanned = 0, existed = 0, saved = 0;

        List<Models.CVE> targets;
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var search = await _provider.SearchCvesAsync(keyword);
            targets = search.ToList();
        }
        else
        {
            var list = await _provider.GetLatestCvesAsync(latest);
            targets = list.ToList();
        }

        scanned = targets.Count;

        using var throttler = new SemaphoreSlim(_maxParallelism);
        var tasks = new List<Task>();

        foreach (var cve in targets)
        {
            var path = CacheFileForId(cve.Id);
            bool alreadyExists = File.Exists(path);
            if (alreadyExists && !force)
            {
                existed++;
                continue;
            }

            if (alreadyExists && force)
            {
                // Conta como existente e reprocessa/salva
                existed++;
            }

            tasks.Add(Task.Run(async () =>
            {
                await throttler.WaitAsync(ct);
                try
                {
                    var detailed = await _provider.GetCveByIdAsync(cve.Id);
                    if (detailed != null)
                    {
                        Interlocked.Increment(ref saved);
                    }
                }
                catch (Exception ex)
                {
                    errors.Add($"{cve.Id}: {ex.Message}");
                }
                finally
                {
                    throttler.Release();
                }
            }, ct));
        }

        await Task.WhenAll(tasks);

        var mode = string.IsNullOrWhiteSpace(keyword) ? $"latest:{latest}" : $"keyword:{keyword}";
        return new CveSyncResult(mode, scanned, existed, saved, errors.ToList());
    }
}
