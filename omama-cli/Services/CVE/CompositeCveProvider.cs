namespace omama_cli.Services.CVE;

public class CompositeCveProvider : ICveDataProvider
{
    private readonly IEnumerable<ICveDataProvider> _providers;
    private readonly ICveMergeStrategy _mergeStrategy;

    public CompositeCveProvider(IEnumerable<ICveDataProvider> providers, ICveMergeStrategy? mergeStrategy = null)
    {
        _providers = providers;
        _mergeStrategy = mergeStrategy ?? new DefaultCveMergeStrategy();
    }

    public async Task<Models.CVE?> GetCveByIdAsync(string cveId)
    {
        var tasks = _providers.Select(p => p.GetCveByIdAsync(cveId));
        var results = await Task.WhenAll(tasks);
        
        var validResults = results.Where(r => r != null).ToList();
        if (!validResults.Any())
            return null;

        return _mergeStrategy.MergeCves(validResults!);
    }

    public async Task<IEnumerable<Models.CVE>> SearchCvesAsync(string keyword)
    {
        var tasks = _providers.Select(p => p.SearchCvesAsync(keyword));
        var results = await Task.WhenAll(tasks);
        
        var allCves = results.SelectMany(r => r);
        return _mergeStrategy.MergeCvesList(allCves);
    }

    public async Task<IEnumerable<Models.CVE>> GetLatestCvesAsync(int limit = 10)
    {
        var tasks = _providers.Select(p => p.GetLatestCvesAsync(limit));
        var results = await Task.WhenAll(tasks);
        
        var allCves = results.SelectMany(r => r);
        return _mergeStrategy.MergeCvesList(allCves).Take(limit);
    }
}