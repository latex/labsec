using System.Text.Json;
using System.Text.Json.Serialization;
using System.Diagnostics;

namespace omama_cli.Services.CVE;

public class CircleCveProvider : ICveDataProvider, IProvidesCveCount
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;
    private static bool _loggingEnabled = Environment.GetEnvironmentVariable("OMAMA_STAT") == "homol";

    public CircleCveProvider(HttpClient httpClient, string? baseUrl = null)
    {
        _httpClient = httpClient;
        _baseUrl = baseUrl 
            ?? Environment.GetEnvironmentVariable("CIRCL_CVE_API_BASE_URL") 
            ?? throw new InvalidOperationException("CIRCL_CVE_API_BASE_URL environment variable is not set and no baseUrl was provided.");
        Log($"CIRCL Provider inicializado com base URL: {_baseUrl}");
    }

    private static void Log(string message)
    {
        if (_loggingEnabled)
            Console.WriteLine($"[CIRCL] {DateTime.Now:HH:mm:ss.fff} {message}");
    }

    public async Task<Models.CVE?> GetCveByIdAsync(string cveId)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            Log($"Buscando CVE {cveId} no CIRCL...");
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(12));
            var response = await _httpClient.GetStringAsync($"{_baseUrl}/cve/{cveId}", cts.Token);
            
            TelemetryService.RecordHttpMetric("CIRCL", $"{_baseUrl}/cve/{cveId}", 200, sw.Elapsed, response.Length);
            
                    var circleCve = JsonSerializer.Deserialize<CircleCveResponse>(response);
            
            if (circleCve == null)
            {
                TelemetryService.RecordMetric("GetCveById", "CIRCL", sw.Elapsed, false, "No CVE data found",
                    new Dictionary<string, object> { ["CveId"] = cveId });
                return null;
            }

            var result = new Models.CVE
            {
                Id = circleCve.Id,
                Description = circleCve.Details,
                PublishedDate = circleCve.Published,
                LastModifiedDate = circleCve.Modified,
                Score = ExtractCvssScore(circleCve.Severity),
                Severity = ExtractSeverityLevel(circleCve.Severity),
                References = circleCve.References?.Select(r => r.Url).ToList() ?? new List<string>()
            };
            
            TelemetryService.RecordMetric("GetCveById", "CIRCL", sw.Elapsed, true, null,
                new Dictionary<string, object> { ["CveId"] = cveId });
            Log($"CVE {cveId} recuperado com sucesso do CIRCL");
            
            return result;
        }
        catch (OperationCanceledException oce)
        {
            TelemetryService.RecordMetric("GetCveById", "CIRCL", sw.Elapsed, false, "Timeout",
                new Dictionary<string, object> { ["CveId"] = cveId });
            Log($"Timeout ao buscar CVE {cveId}: {oce.Message}");
            return null;
        }
        catch (HttpRequestException ex)
        {
            TelemetryService.RecordMetric("GetCveById", "CIRCL", sw.Elapsed, false, ex.Message,
                new Dictionary<string, object> { ["CveId"] = cveId });
            Log($"Erro HTTP ao buscar CVE {cveId}: {ex.Message}");
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
                Description = r.Details,
                PublishedDate = r.Published,
                LastModifiedDate = r.Modified,
                Score = ExtractCvssScore(r.Severity),
                Severity = ExtractSeverityLevel(r.Severity),
                References = r.References?.Select(rf => rf.Url).ToList() ?? new List<string>()
            });
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
            Log($"GetLatestCves chamado com limit inválido: {limit}");
            return Array.Empty<Models.CVE>();
        }

        var sw = Stopwatch.StartNew();
        try
        {
            var url = $"{_baseUrl}/last/{limit}";
            Log($"Fazendo requisição GetLatest: {url}");
            
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(12));
            var response = await _httpClient.GetStringAsync(url, cts.Token);
            Log($"GetLatest resposta recebida. Tamanho: {response.Length} caracteres");
            
                TelemetryService.RecordHttpMetric("CIRCL", url, 200, sw.Elapsed, response.Length);
            
                    var results = JsonSerializer.Deserialize<List<CircleCveResponse>>(response) ?? new();
            Log($"GetLatest parsing completo. CVEs encontrados: {results.Count}");
            
            var converted = results.Select(r => new Models.CVE
            {
                Id = r.Id,
                Description = r.Details,
                PublishedDate = r.Published,
                LastModifiedDate = r.Modified,
                Score = ExtractCvssScore(r.Severity),
                Severity = ExtractSeverityLevel(r.Severity),
                References = r.References?.Select(rf => rf.Url).ToList() ?? new List<string>()
            }).ToList();
            
            Log($"GetLatest conversão completa. CVEs convertidos: {converted.Count}");
            
            TelemetryService.RecordMetric("GetLatestCves", "CIRCL", sw.Elapsed, true, null,
                new Dictionary<string, object> { 
                    ["Limit"] = limit, 
                    ["ResultCount"] = converted.Count 
                });
                
            return converted;
        }
        catch (OperationCanceledException oce)
        {
            Log($"Timeout em GetLatestCves: {oce.Message}");
            TelemetryService.RecordMetric("GetLatestCves", "CIRCL", sw.Elapsed, false, "Timeout",
                new Dictionary<string, object> { ["Limit"] = limit });
            return Array.Empty<Models.CVE>();
        }
        catch (Exception ex)
        {
            Log($"ERRO em GetLatestCves: {ex.GetType().Name} - {ex.Message}");
            TelemetryService.RecordMetric("GetLatestCves", "CIRCL", sw.Elapsed, false, ex.Message,
                new Dictionary<string, object> { ["Limit"] = limit });
            return Array.Empty<Models.CVE>();
        }
        finally
        {
            sw.Stop();
            Log($"GetLatestCves(limit={limit}) completou em {sw.ElapsedMilliseconds}ms");
        }
    }

    private static double ExtractCvssScore(List<CirclSeverity>? severities)
    {
        if (severities == null) return 0.0;
        
        foreach (var severity in severities)
        {
            if (severity.Type.Contains("CVSS", StringComparison.OrdinalIgnoreCase))
            {
                // Parse CVSS:3.1/AV:N/AC:L/PR:N/UI:N/S:U/C:N/I:N/A:N format
                var parts = severity.Score.Split('/');
                if (parts.Length > 0 && parts[0].Contains(':'))
                {
                    var scorePart = parts[0].Split(':');
                    if (scorePart.Length > 1 && double.TryParse(scorePart[1], out var score))
                    {
                        return score;
                    }
                }
            }
        }
        return 0.0;
    }

    private static string ExtractSeverityLevel(List<CirclSeverity>? severities)
    {
        if (severities == null) return "NONE";
        
        var score = ExtractCvssScore(severities);
        return score switch
        {
            >= 9.0 => "CRITICAL",
            >= 7.0 => "HIGH",
            >= 4.0 => "MEDIUM",
            > 0.0 => "LOW",
            _ => "NONE"
        };
    }

    public Task<long?> GetTotalCountAsync(CancellationToken cancellationToken = default)
    {
        // CIRCL API doesn't expose an easy total count endpoint. Return null to indicate unknown.
        return Task.FromResult<long?>(null);
    }
}

public class CircleCveResponse
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("details")]
    public string Details { get; set; } = string.Empty;

    [JsonPropertyName("references")]
    public List<CirclReference>? References { get; set; }

    [JsonPropertyName("published")]
    public DateTime Published { get; set; }

    [JsonPropertyName("modified")]
    public DateTime Modified { get; set; }

    [JsonPropertyName("severity")]
    public List<CirclSeverity>? Severity { get; set; }
}

public class CirclReference
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;
}

public class CirclSeverity
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("score")]
    public string Score { get; set; } = string.Empty;
}

public class CircleCveReference
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;
}

public class CircleSeverity
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("score")]
    public string Score { get; set; } = string.Empty;
}