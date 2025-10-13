namespace omama_cli.Services.CVE;

public class CveDetailsCveProvider : ICveDataProvider, IProvidesCveCount
{
    private readonly HttpClient _http;
    private readonly string _baseUrl;

    public CveDetailsCveProvider(HttpClient httpClient, string? baseUrl = null)
    {
        _http = httpClient;
        _baseUrl = baseUrl ?? "https://www.cvedetails.com/";
    }

    public Task<omama_cli.Models.CVE?> GetCveByIdAsync(string cveId) => Task.FromResult<omama_cli.Models.CVE?>(null);
    public Task<IEnumerable<omama_cli.Models.CVE>> SearchCvesAsync(string keyword) => Task.FromResult<IEnumerable<omama_cli.Models.CVE>>(Array.Empty<omama_cli.Models.CVE>());
    public Task<IEnumerable<omama_cli.Models.CVE>> GetLatestCvesAsync(int limit = 10) => Task.FromResult<IEnumerable<omama_cli.Models.CVE>>(Array.Empty<omama_cli.Models.CVE>());
    public Task<long?> GetTotalCountAsync(CancellationToken cancellationToken = default) => Task.FromResult<long?>(null);
}
