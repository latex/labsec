namespace omama_cli.Services.CVE;

public record SourceCount(string Name, long? Total);

public class CveSourceStatsService
{
    private readonly IEnumerable<NamedCveProvider> _providers;

    public CveSourceStatsService(IEnumerable<NamedCveProvider> providers)
    {
        _providers = providers;
    }

    public async Task<IReadOnlyList<SourceCount>> GetCountsAsync(CancellationToken cancellationToken = default)
    {
        var list = new List<SourceCount>();
        foreach (var np in _providers)
        {
            long? total = null;
            if (np.Provider is IProvidesCveCount counter)
            {
                total = await counter.GetTotalCountAsync(cancellationToken);
            }
            list.Add(new SourceCount(np.Name, total));
        }
        return list;
    }
}
