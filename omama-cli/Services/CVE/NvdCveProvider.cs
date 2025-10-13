using System.Text.Json;

namespace omama_cli.Services.CVE;

public class NvdCveProvider : ICveDataProvider, IProvidesCveCount
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;

    public NvdCveProvider(HttpClient httpClient, string? baseUrl = null)
    {
        _httpClient = httpClient;
        _baseUrl = baseUrl ?? "https://services.nvd.nist.gov/rest/json/cves/2.0";
    }

    private static Models.CVE ConvertToCve(Models.NVD.NvdVulnerability vulnerability)
    {
        return new Models.CVE
        {
            Id = vulnerability.Cve.Id,
            Description = vulnerability.Cve.Descriptions.FirstOrDefault()?.Value ?? string.Empty,
            PublishedDate = vulnerability.Cve.Published,
            LastModifiedDate = vulnerability.Cve.LastModified,
            Score = vulnerability.Cve.Metrics?.CvssMetricV31.FirstOrDefault()?.CvssData.BaseScore ?? 0.0,
            Severity = vulnerability.Cve.Metrics?.CvssMetricV31.FirstOrDefault()?.CvssData.BaseSeverity ?? "NONE",
            References = vulnerability.Cve.References.Select(r => r.Url).ToList()
        };
    }

    private static bool IsJson(HttpResponseMessage resp)
        => resp.Content.Headers.ContentType?.MediaType?.Contains("json", StringComparison.OrdinalIgnoreCase) == true;

    private async Task<T?> ReadJsonAsync<T>(HttpResponseMessage resp, CancellationToken ct = default)
    {
        try
        {
            await using var stream = await resp.Content.ReadAsStreamAsync(ct);
            return await JsonSerializer.DeserializeAsync<T>(stream, cancellationToken: ct);
        }
        catch (JsonException)
        {
            return default;
        }
    }

    public async Task<Models.CVE?> GetCveByIdAsync(string cveId)
    {
        if (string.IsNullOrWhiteSpace(cveId))
        {
            return null;
        }

        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, $"{_baseUrl}?cveId={cveId}");
            req.Headers.UserAgent.ParseAdd("omama-cli/1.0");
            using var resp = await _httpClient.SendAsync(req);
            if (!resp.IsSuccessStatusCode || !IsJson(resp)) return null;
            var nvdResponse = await ReadJsonAsync<Models.NVD.NvdResponse>(resp);
            
            var vulnerability = nvdResponse?.Vulnerabilities.FirstOrDefault();
            return vulnerability == null ? null : ConvertToCve(vulnerability);
        }
        catch
        {
            return null;
        }
    }

    public async Task<IEnumerable<Models.CVE>> SearchCvesAsync(string keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword))
        {
            return Array.Empty<Models.CVE>();
        }

        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, $"{_baseUrl}?keywordSearch={keyword}");
            req.Headers.UserAgent.ParseAdd("omama-cli/1.0");
            using var resp = await _httpClient.SendAsync(req);
            if (!resp.IsSuccessStatusCode || !IsJson(resp)) return Array.Empty<Models.CVE>();
            var nvdResponse = await ReadJsonAsync<Models.NVD.NvdResponse>(resp);
            return nvdResponse?.Vulnerabilities.Select(ConvertToCve) ?? Array.Empty<Models.CVE>();
        }
        catch
        {
            return Array.Empty<Models.CVE>();
        }
    }

    public async Task<IEnumerable<Models.CVE>> GetLatestCvesAsync(int limit = 10)
    {
        if (limit <= 0)
        {
            return Array.Empty<Models.CVE>();
        }

        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, $"{_baseUrl}?resultsPerPage={limit}");
            req.Headers.UserAgent.ParseAdd("omama-cli/1.0");
            using var resp = await _httpClient.SendAsync(req);
            if (!resp.IsSuccessStatusCode || !IsJson(resp)) return Array.Empty<Models.CVE>();
            var nvdResponse = await ReadJsonAsync<Models.NVD.NvdResponse>(resp);
            return nvdResponse?.Vulnerabilities.Select(ConvertToCve) ?? Array.Empty<Models.CVE>();
        }
        catch
        {
            return Array.Empty<Models.CVE>();
        }
    }

    public async Task<long?> GetTotalCountAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, $"{_baseUrl}?resultsPerPage=1");
            req.Headers.UserAgent.ParseAdd("omama-cli/1.0");
            using var resp = await _httpClient.SendAsync(req, cancellationToken);
            if (!resp.IsSuccessStatusCode || !IsJson(resp)) return null;
            var nvdResponse = await ReadJsonAsync<omama_cli.Models.NVD.NvdResponse>(resp, cancellationToken);
            return nvdResponse?.TotalResults;
        }
        catch
        {
            return null;
        }
    }
}