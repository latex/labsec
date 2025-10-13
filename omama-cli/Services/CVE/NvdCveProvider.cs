using System.Text.Json;

namespace omama_cli.Services.CVE;

public class NvdCveProvider : ICveDataProvider
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

    public async Task<Models.CVE?> GetCveByIdAsync(string cveId)
    {
        if (string.IsNullOrWhiteSpace(cveId))
        {
            return null;
        }

        try
        {
            var response = await _httpClient.GetStringAsync($"{_baseUrl}?cveId={cveId}");
            var nvdResponse = JsonSerializer.Deserialize<Models.NVD.NvdResponse>(response);
            
            var vulnerability = nvdResponse?.Vulnerabilities.FirstOrDefault();
            return vulnerability == null ? null : ConvertToCve(vulnerability);
        }
        catch (HttpRequestException)
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
            var response = await _httpClient.GetStringAsync($"{_baseUrl}?keywordSearch={keyword}");
            var nvdResponse = JsonSerializer.Deserialize<Models.NVD.NvdResponse>(response);
            
            return nvdResponse?.Vulnerabilities.Select(ConvertToCve) ?? Array.Empty<Models.CVE>();
        }
        catch (HttpRequestException)
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
            var response = await _httpClient.GetStringAsync($"{_baseUrl}?resultsPerPage={limit}");
            var nvdResponse = JsonSerializer.Deserialize<Models.NVD.NvdResponse>(response);
            
            return nvdResponse?.Vulnerabilities.Select(ConvertToCve) ?? Array.Empty<Models.CVE>();
        }
        catch (HttpRequestException)
        {
            return Array.Empty<Models.CVE>();
        }
    }
}