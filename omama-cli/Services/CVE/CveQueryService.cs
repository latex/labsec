using System.Globalization;
using omama_cli.Models;
using CVEModel = omama_cli.Models.CVE;

namespace omama_cli.Services.CVE;

public record CveQueryOptions(
    string? Q = null,
    DateTimeOffset? Since = null,
    DateTimeOffset? Until = null,
    IReadOnlyCollection<string>? Severities = null,
    double? MinScore = null,
    double? MaxScore = null,
    string SortBy = "published",
    bool Desc = true,
    int Page = 1,
    int PageSize = 20);

public record CveQueryResult(
    int Total,
    int Page,
    int PageSize,
    IReadOnlyList<CVEModel> Items);

public class CveQueryService
{
    /// Conta o total de CVEs salvos por fonte no cache local
    public async Task<Dictionary<string, int>> CountSavedBySourceAsync()
    {
        var cache = new HighPerformanceCache(_cacheDir);
        var files = Directory.GetFiles(_cacheDir, "*.json.gz");
        var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var file in files)
        {
            var key = Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(file));
            if (!key.StartsWith("cve:id:", StringComparison.OrdinalIgnoreCase))
                continue;
            try
            {
                var item = await cache.GetAsync<CVEModel>(key);
                if (item != null)
                {
                    // Usa o campo Source se existir, senão "desconhecido"
                    var source = item.Source ?? "desconhecido";
                    if (!counts.ContainsKey(source))
                        counts[source] = 0;
                    counts[source]++;
                }
            }
            catch
            {
                // Ignora entradas malformadas
            }
        }
        return counts;
    }
    private readonly string _cacheDir;

    public CveQueryService(string cacheDir)
    {
        _cacheDir = cacheDir;
        Directory.CreateDirectory(_cacheDir);
    }

    public async Task<CveQueryResult> ListSavedAsync(CveQueryOptions options, CancellationToken ct = default)
    {
        var cache = new HighPerformanceCache(_cacheDir);
        var items = new List<CVEModel>();

        // Load only CVE entries saved by ID (skip search/latest aggregate caches)
        var files = Directory.GetFiles(_cacheDir, "*.json.gz");
        foreach (var file in files)
        {
            // key without extensions
            var key = Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(file));
            if (!key.StartsWith("cve:id:", StringComparison.OrdinalIgnoreCase))
                continue;

            try
            {
                var item = await cache.GetAsync<CVEModel>(key);
                if (item != null)
                    items.Add(item);
            }
            catch
            {
                // Ignore malformed entries
            }
        }

        IEnumerable<CVEModel> q = items;

        if (!string.IsNullOrWhiteSpace(options.Q))
        {
            var needle = options.Q.Trim();
            q = q.Where(c => (c.Id?.Contains(needle, StringComparison.OrdinalIgnoreCase) ?? false)
                           || (c.Description?.Contains(needle, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        if (options.Since.HasValue)
            q = q.Where(c => c.PublishedDate >= options.Since.Value.UtcDateTime);

        if (options.Until.HasValue)
            q = q.Where(c => c.PublishedDate <= options.Until.Value.UtcDateTime);

        if (options.Severities != null && options.Severities.Count > 0)
        {
            var set = new HashSet<string>(options.Severities.Select(s => s.ToUpperInvariant()));
            q = q.Where(c => !string.IsNullOrWhiteSpace(c.Severity) && set.Contains(c.Severity.ToUpperInvariant()));
        }

        if (options.MinScore.HasValue)
            q = q.Where(c => c.Score >= options.MinScore.Value);

        if (options.MaxScore.HasValue)
            q = q.Where(c => c.Score <= options.MaxScore.Value);

        q = (options.SortBy.ToLowerInvariant()) switch
        {
            "published" => options.Desc ? q.OrderByDescending(c => c.PublishedDate) : q.OrderBy(c => c.PublishedDate),
            "modified"  => options.Desc ? q.OrderByDescending(c => c.LastModifiedDate) : q.OrderBy(c => c.LastModifiedDate),
            "score"     => options.Desc ? q.OrderByDescending(c => c.Score) : q.OrderBy(c => c.Score),
            "id"        => options.Desc ? q.OrderByDescending(c => c.Id) : q.OrderBy(c => c.Id),
            _            => q
        };

        var total = q.Count();
        var skip = Math.Max(0, (options.Page - 1) * options.PageSize);
        var pageItems = q.Skip(skip).Take(options.PageSize).ToList();

        return new CveQueryResult(total, options.Page, options.PageSize, pageItems);
    }
}
