using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace omama_cli.Services.CVE;

public class CveDetailsCveProvider : ICveDataProvider, IProvidesCveCount
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;

    public CveDetailsCveProvider(HttpClient httpClient, string? baseUrl = null)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        // Fallback to catalog URL when not provided
        _baseUrl = string.IsNullOrWhiteSpace(baseUrl) ? CveSourceCatalog.CVEDETAILS_URL : baseUrl;
    }

    public Task<omama_cli.Models.CVE?> GetCveByIdAsync(string cveId) => Task.FromResult<omama_cli.Models.CVE?>(null);
    public Task<IEnumerable<omama_cli.Models.CVE>> SearchCvesAsync(string keyword) => Task.FromResult<IEnumerable<omama_cli.Models.CVE>>(Array.Empty<omama_cli.Models.CVE>());
    public Task<IEnumerable<omama_cli.Models.CVE>> GetLatestCvesAsync(int limit = 10) => Task.FromResult<IEnumerable<omama_cli.Models.CVE>>(Array.Empty<omama_cli.Models.CVE>());
    public Task<long?> GetTotalCountAsync(CancellationToken cancellationToken = default) => Task.FromResult<long?>(null);
}
