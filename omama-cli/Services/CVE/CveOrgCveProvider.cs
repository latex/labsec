using System.Text.Json;

namespace omama_cli.Services.CVE;

public class CveOrgCveProvider : ICveDataProvider
{
    private readonly HttpClient _http;
    private readonly string _baseUrl;

    public CveOrgCveProvider(HttpClient httpClient, string? baseUrl = null)
    {
        _http = httpClient;
        // Public CVE Services (read-only) endpoint; subject to change
        _baseUrl = baseUrl ?? "https://cveawg.mitre.org/api/cve";
    }

    public async Task<omama_cli.Models.CVE?> GetCveByIdAsync(string cveId)
    {
        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, $"{_baseUrl}/{cveId}");
            using var resp = await _http.SendAsync(req);
            if (!resp.IsSuccessStatusCode) return null;
            await using var stream = await resp.Content.ReadAsStreamAsync();
            var doc = await JsonDocument.ParseAsync(stream);
            var root = doc.RootElement;
            if (root.ValueKind == JsonValueKind.Object)
            {
                var id = root.GetProperty("cveMetadata").GetProperty("cveId").GetString() ?? cveId;
                var desc = root.GetProperty("containers").GetProperty("cna").GetProperty("descriptions")[0].GetProperty("value").GetString() ?? string.Empty;
                return new omama_cli.Models.CVE
                {
                    Id = id,
                    Description = desc,
                    PublishedDate = DateTime.UtcNow,
                    LastModifiedDate = DateTime.UtcNow,
                    Score = 0.0,
                    Severity = "NONE",
                    References = new List<string>()
                };
            }
            return null;
        }
        catch { return null; }
    }

    public Task<IEnumerable<omama_cli.Models.CVE>> SearchCvesAsync(string keyword)
        => Task.FromResult<IEnumerable<omama_cli.Models.CVE>>(Array.Empty<omama_cli.Models.CVE>());

    public Task<IEnumerable<omama_cli.Models.CVE>> GetLatestCvesAsync(int limit = 10)
        => Task.FromResult<IEnumerable<omama_cli.Models.CVE>>(Array.Empty<omama_cli.Models.CVE>());
}
