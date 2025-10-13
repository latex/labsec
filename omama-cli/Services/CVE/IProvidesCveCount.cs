namespace omama_cli.Services.CVE;

public interface IProvidesCveCount
{
    /// Returns the total number of CVEs known by this source, or null if unsupported.
    Task<long?> GetTotalCountAsync(CancellationToken cancellationToken = default);
}
