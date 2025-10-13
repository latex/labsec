using System.Text.Json;
using System.Text.Json.Serialization;

namespace omama_cli.Services.CVE;

public class CircleCveProvider : ICveDataProvider
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;

    public CircleCveProvider(HttpClient httpClient, string? baseUrl = null)
    {
        _httpClient = httpClient;
        _baseUrl = baseUrl ?? "https://api.circl.lu/v1/cve";
    }

    public async Task<Models.CVE?> GetCveByIdAsync(string cveId)
    {
        try
        {
            var response = await _httpClient.GetStringAsync($"{_baseUrl}/{cveId}");
            var circleCve = JsonSerializer.Deserialize<CircleCveResponse>(response);
            
            if (circleCve == null)
                return null;

            return new Models.CVE
            {
                Id = circleCve.Id,
                Description = circleCve.Summary,
                PublishedDate = circleCve.Published,
                LastModifiedDate = circleCve.Modified,
                Score = circleCve.Cvss ?? 0.0,
                Severity = MapSeverity(circleCve.Cvss ?? 0.0),
                References = circleCve.References ?? new List<string>()
            };
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    public async Task<IEnumerable<Models.CVE>> SearchCvesAsync(string keyword)
    {
        try
        {
            var response = await _httpClient.GetStringAsync($"{_baseUrl}/search/{keyword}");
            var results = JsonSerializer.Deserialize<List<CircleCveResponse>>(response) ?? new();
            
            return results.Select(r => new Models.CVE
            {
                Id = r.Id,
                Description = r.Summary,
                PublishedDate = r.Published,
                LastModifiedDate = r.Modified,
                Score = r.Cvss ?? 0.0,
                Severity = MapSeverity(r.Cvss ?? 0.0),
                References = r.References ?? new List<string>()
            });
        }
        catch (HttpRequestException)
        {
            return Array.Empty<Models.CVE>();
        }
    }

    public async Task<IEnumerable<Models.CVE>> GetLatestCvesAsync(int limit = 10)
    {
        try
        {
            var response = await _httpClient.GetStringAsync($"{_baseUrl}/last/{limit}");
            var results = JsonSerializer.Deserialize<List<CircleCveResponse>>(response) ?? new();
            
            return results.Select(r => new Models.CVE
            {
                Id = r.Id,
                Description = r.Summary,
                PublishedDate = r.Published,
                LastModifiedDate = r.Modified,
                Score = r.Cvss ?? 0.0,
                Severity = MapSeverity(r.Cvss ?? 0.0),
                References = r.References ?? new List<string>()
            });
        }
        catch (HttpRequestException)
        {
            return Array.Empty<Models.CVE>();
        }
    }

    private static string MapSeverity(double cvssScore)
    {
        return cvssScore switch
        {
            >= 9.0 => "CRITICAL",
            >= 7.0 => "HIGH",
            >= 4.0 => "MEDIUM",
            > 0.0 => "LOW",
            _ => "NONE"
        };
    }
}

public class CircleCveResponse
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("summary")]
    public string Summary { get; set; } = string.Empty;

    [JsonPropertyName("references")]
    public List<string>? References { get; set; }

    [JsonPropertyName("Published")]
    public DateTime Published { get; set; }

    [JsonPropertyName("Modified")]
    public DateTime Modified { get; set; }

    [JsonPropertyName("cvss")]
    public double? Cvss { get; set; }
}