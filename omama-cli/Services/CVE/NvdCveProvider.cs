using System.Text.Json;
using System.Diagnostics;

namespace omama_cli.Services.CVE;

public class NvdCveProvider : ICveDataProvider, IProvidesCveCount
{
    private const string OPERATION_GET_CVE_BY_ID = "GetCveById";
    private const string OPERATION_GET_LATEST_CVES = "GetLatestCves";
    private const string PROVIDER_NAME = "NVD";
    
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;
    private static bool _loggingEnabled = Environment.GetEnvironmentVariable("OMAMA_STAT") == "homol";

    public NvdCveProvider(HttpClient httpClient, string? baseUrl = null)
    {
        _httpClient = httpClient;
        _baseUrl = baseUrl ?? "https://services.nvd.nist.gov/rest/json/cves/2.0";
        Log($"NVD Provider inicializado com base URL: {_baseUrl}");
    }

    private static void Log(string message)
    {
        if (_loggingEnabled)
            Console.WriteLine($"[NVD] {DateTime.Now:HH:mm:ss.fff} {message}");
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
            Log($"GetCveById chamado com ID vazio/nulo");
            TelemetryService.RecordMetric("GetCveById", "NVD", TimeSpan.Zero, false, "Empty CVE ID");
            return null;
        }

        var sw = Stopwatch.StartNew();
        try
        {
            var url = $"{_baseUrl}?cveId={cveId}";
            Log($"Fazendo requisição GET: {url}");
            
            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.UserAgent.ParseAdd("omama-cli/1.0");
            using var resp = await _httpClient.SendAsync(req);
            
            Log($"Resposta HTTP: {resp.StatusCode} - Content-Type: {resp.Content.Headers.ContentType?.MediaType} - Tamanho: {resp.Content.Headers.ContentLength}");
            
            // Record HTTP telemetry
                TelemetryService.RecordHttpMetric("NVD", url, (int)resp.StatusCode, sw.Elapsed, resp.Content.Headers.ContentLength);
            
            if (!resp.IsSuccessStatusCode || !IsJson(resp))
            {
                Log($"Resposta inválida para {cveId}: Status={resp.StatusCode}, IsJson={IsJson(resp)}");
                TelemetryService.RecordMetric("GetCveById", "NVD", sw.Elapsed, false, $"HTTP {resp.StatusCode}");
                return null;
            }
            
            var nvdResponse = await ReadJsonAsync<Models.NVD.NvdResponse>(resp);
            Log($"Parsing JSON concluído para {cveId}. Vulnerabilidades encontradas: {nvdResponse?.Vulnerabilities.Count ?? 0}");
            
            var vulnerability = nvdResponse?.Vulnerabilities.FirstOrDefault();
            var result = vulnerability == null ? null : ConvertToCve(vulnerability);
            Log($"CVE {cveId} convertido com sucesso: {result?.Id}");
            
            TelemetryService.RecordMetric("GetCveById", "NVD", sw.Elapsed, result != null, 
                result == null ? "No vulnerability found" : null,
                new Dictionary<string, object> { ["CveId"] = cveId });
            
            return result;
        }
        catch (Exception ex)
        {
            Log($"ERRO ao buscar CVE {cveId}: {ex.GetType().Name} - {ex.Message}");
            return null;
        }
        finally
        {
            sw.Stop();
            Log($"GetCveById({cveId}) completou em {sw.ElapsedMilliseconds}ms");
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
            Log($"GetLatestCves chamado com limit inválido: {limit}");
            return Array.Empty<Models.CVE>();
        }

        var sw = Stopwatch.StartNew();
        try
        {
            var url = $"{_baseUrl}?resultsPerPage={limit}";
            Log($"Fazendo requisição GetLatest: {url}");
            
            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.UserAgent.ParseAdd("omama-cli/1.0");
            using var resp = await _httpClient.SendAsync(req);
            
            Log($"GetLatest resposta: {resp.StatusCode} - Content-Type: {resp.Content.Headers.ContentType?.MediaType} - Tamanho: {resp.Content.Headers.ContentLength}");
            
            // Record HTTP telemetry
                TelemetryService.RecordHttpMetric("NVD", url, (int)resp.StatusCode, sw.Elapsed, resp.Content.Headers.ContentLength);
            
            if (!resp.IsSuccessStatusCode || !IsJson(resp))
            {
                Log($"GetLatest falhou: Status={resp.StatusCode}, IsJson={IsJson(resp)}");
                TelemetryService.RecordMetric("GetLatestCves", "NVD", sw.Elapsed, false, $"HTTP {resp.StatusCode}",
                    new Dictionary<string, object> { ["Limit"] = limit });
                return Array.Empty<Models.CVE>();
            }
            
            var nvdResponse = await ReadJsonAsync<Models.NVD.NvdResponse>(resp);
            var vulnerabilities = nvdResponse?.Vulnerabilities ?? new List<Models.NVD.NvdVulnerability>();
            Log($"GetLatest parsing completo. CVEs encontrados: {vulnerabilities.Count}");
            
            var result = vulnerabilities.Select(ConvertToCve).ToList();
            Log($"GetLatest conversão completa. CVEs convertidos: {result.Count}");
            
            TelemetryService.RecordMetric("GetLatestCves", "NVD", sw.Elapsed, true, null,
                new Dictionary<string, object> { 
                    ["Limit"] = limit, 
                    ["ResultCount"] = result.Count 
                });
                
            return result;
        }
        catch (Exception ex)
        {
            Log($"ERRO em GetLatestCves: {ex.GetType().Name} - {ex.Message}");
            TelemetryService.RecordMetric("GetLatestCves", "NVD", sw.Elapsed, false, ex.Message,
                new Dictionary<string, object> { ["Limit"] = limit });
            return Array.Empty<Models.CVE>();
        }
        finally
        {
            sw.Stop();
            Log($"GetLatestCves(limit={limit}) completou em {sw.ElapsedMilliseconds}ms");
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