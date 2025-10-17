using System.Text.Json;
using System.Diagnostics;

namespace omama_cli.Services.CVE;

public class NvdCveProvider : ICveDataProvider, IProvidesCveCount
{
    // Use a configuração centralizada para o endpoint NVD
    private static readonly string DEFAULT_NVD_URL = omama_cli.Services.CVE.CveSourceCatalog.NVD_URL;
    private const string USER_AGENT = "omama-cli/1.0";
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;
    private static readonly bool LoggingEnabled = Environment.GetEnvironmentVariable("OMAMA_STAT") == "homol";

    public NvdCveProvider(HttpClient httpClient, string? baseUrl = null)
    {
        _httpClient = httpClient;
        _baseUrl = ResolveBaseUrl(baseUrl);
        LogInfo($"NVD Provider inicializado com base URL: {_baseUrl}");
    }

    private static string ResolveBaseUrl(string? baseUrl)
        => baseUrl ?? Environment.GetEnvironmentVariable("OMAMA_NVD_URL") ?? DEFAULT_NVD_URL;

    private static void LogInfo(string message)
    {
        if (LoggingEnabled)
            Console.WriteLine($"[NVD] {DateTime.Now:HH:mm:ss.fff} {message}");
    }

    private static Models.CVE ConvertToCve(Models.NVD.NvdVulnerability vulnerability)
    {
        if (vulnerability?.Cve == null)
            return new Models.CVE();
        var metrics = vulnerability.Cve.Metrics?.CvssMetricV31?.FirstOrDefault()?.CvssData;
        return new Models.CVE
        {
            Id = vulnerability.Cve.Id,
            Description = vulnerability.Cve.Descriptions?.FirstOrDefault()?.Value ?? string.Empty,
            PublishedDate = vulnerability.Cve.Published,
            LastModifiedDate = vulnerability.Cve.LastModified,
            Score = metrics?.BaseScore ?? 0.0,
            Severity = metrics?.BaseSeverity ?? "NONE",
            References = vulnerability.Cve.References?.Select(r => r.Url).ToList() ?? new List<string>()
        };
    }

    private static bool IsJson(HttpResponseMessage resp)
        => resp.Content.Headers.ContentType?.MediaType?.Contains("json", StringComparison.OrdinalIgnoreCase) == true;

    private static async Task<T?> ReadJsonAsync<T>(HttpResponseMessage resp, CancellationToken ct = default)
    {
        try
        {
            await using var stream = await resp.Content.ReadAsStreamAsync(ct);
            return await JsonSerializer.DeserializeAsync<T>(stream, cancellationToken: ct);
        }
        catch (JsonException ex)
        {
            LogJsonError(typeof(T).Name, ex);
            return default;
        }
    }

    private static void LogJsonError(string typeName, Exception ex)
    {
        if (LoggingEnabled)
            Console.WriteLine($"[NVD][JSON] Erro ao deserializar {typeName}: {ex.Message}");
    }

    public async Task<Models.CVE?> GetCveByIdAsync(string cveId)
    {
        if (string.IsNullOrWhiteSpace(cveId))
        {
            LogInfo($"GetCveById chamado com ID vazio/nulo");
            TelemetryService.RecordMetric("GetCveById", "NVD", TimeSpan.Zero, false, "Empty CVE ID");
            return null;
        }

        var sw = Stopwatch.StartNew();
        var url = $"{_baseUrl}?cveId={cveId}";
        LogInfo($"Fazendo requisição GET: {url}");
        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.UserAgent.ParseAdd(USER_AGENT);
            using var resp = await _httpClient.SendAsync(req);

            LogInfo($"Resposta HTTP: {resp.StatusCode} - Content-Type: {resp.Content.Headers.ContentType?.MediaType} - Tamanho: {resp.Content.Headers.ContentLength}");
            TelemetryService.RecordHttpMetric("NVD", url, (int)resp.StatusCode, sw.Elapsed, resp.Content.Headers.ContentLength);

            if (!resp.IsSuccessStatusCode || !IsJson(resp))
            {
                LogInfo($"Resposta inválida para {cveId}: Status={resp.StatusCode}, IsJson={IsJson(resp)}");
                TelemetryService.RecordMetric("GetCveById", "NVD", sw.Elapsed, false, $"HTTP {resp.StatusCode}");
                return null;
            }

            var nvdResponse = await ReadJsonAsync<Models.NVD.NvdResponse>(resp);
            LogInfo($"Parsing JSON concluído para {cveId}. Vulnerabilidades encontradas: {nvdResponse?.Vulnerabilities.Count ?? 0}");

            var vulnerability = nvdResponse?.Vulnerabilities?.FirstOrDefault();
            var result = vulnerability == null ? null : ConvertToCve(vulnerability);
            LogInfo($"CVE {cveId} convertido com sucesso: {result?.Id}");

            TelemetryService.RecordMetric("GetCveById", "NVD", sw.Elapsed, result != null,
                result == null ? "No vulnerability found" : null,
                new Dictionary<string, object> { ["CveId"] = cveId });

            return result;
        }
        catch (Exception ex)
        {
            LogInfo($"ERRO ao buscar CVE {cveId}: {ex.GetType().Name} - {ex.Message}");
            return null;
        }
        finally
        {
            sw.Stop();
            LogInfo($"GetCveById({cveId}) completou em {sw.ElapsedMilliseconds}ms");
        }
    }

    public async Task<IEnumerable<Models.CVE>> SearchCvesAsync(string keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword))
            return Array.Empty<Models.CVE>();

        var url = $"{_baseUrl}?keywordSearch={keyword}";
        LogInfo($"SearchCvesAsync: {url}");
        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.UserAgent.ParseAdd(USER_AGENT);
            using var resp = await _httpClient.SendAsync(req);
            if (!resp.IsSuccessStatusCode || !IsJson(resp)) return Array.Empty<Models.CVE>();
            var nvdResponse = await ReadJsonAsync<Models.NVD.NvdResponse>(resp);
            return nvdResponse?.Vulnerabilities?.Select(ConvertToCve) ?? Array.Empty<Models.CVE>();
        }
        catch (Exception ex)
        {
            LogInfo($"SearchCvesAsync ERRO: {ex.GetType().Name} - {ex.Message}");
            return Array.Empty<Models.CVE>();
        }
    }

    public async Task<IEnumerable<Models.CVE>> GetLatestCvesAsync(int limit = 10)
    {
        if (limit <= 0)
        {
            LogInfo($"GetLatestCves chamado com limit inválido: {limit}");
            return Array.Empty<Models.CVE>();
        }

        var sw = Stopwatch.StartNew();
        var url = $"{_baseUrl}?resultsPerPage={limit}";
        LogInfo($"Fazendo requisição GetLatest: {url}");
        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.UserAgent.ParseAdd(USER_AGENT);
            using var resp = await _httpClient.SendAsync(req);

            LogInfo($"GetLatest resposta: {resp.StatusCode} - Content-Type: {resp.Content.Headers.ContentType?.MediaType} - Tamanho: {resp.Content.Headers.ContentLength}");
            TelemetryService.RecordHttpMetric("NVD", url, (int)resp.StatusCode, sw.Elapsed, resp.Content.Headers.ContentLength);

            if (!resp.IsSuccessStatusCode || !IsJson(resp))
            {
                LogInfo($"GetLatest falhou: Status={resp.StatusCode}, IsJson={IsJson(resp)}");
                TelemetryService.RecordMetric("GetLatestCves", "NVD", sw.Elapsed, false, $"HTTP {resp.StatusCode}",
                    new Dictionary<string, object> { ["Limit"] = limit });
                return Array.Empty<Models.CVE>();
            }

            var nvdResponse = await ReadJsonAsync<Models.NVD.NvdResponse>(resp);
            var vulnerabilities = nvdResponse?.Vulnerabilities ?? new List<Models.NVD.NvdVulnerability>();
            LogInfo($"GetLatest parsing completo. CVEs encontrados: {vulnerabilities.Count}");

            var result = vulnerabilities.Select(ConvertToCve).ToList();
            LogInfo($"GetLatest conversão completa. CVEs convertidos: {result.Count}");

            TelemetryService.RecordMetric("GetLatestCves", "NVD", sw.Elapsed, true, null,
                new Dictionary<string, object> {
                    ["Limit"] = limit,
                    ["ResultCount"] = result.Count
                });

            return result;
        }
        catch (Exception ex)
        {
            LogInfo($"ERRO em GetLatestCves: {ex.GetType().Name} - {ex.Message}");
            TelemetryService.RecordMetric("GetLatestCves", "NVD", sw.Elapsed, false, ex.Message,
                new Dictionary<string, object> { ["Limit"] = limit });
            return Array.Empty<Models.CVE>();
        }
        finally
        {
            sw.Stop();
            LogInfo($"GetLatestCves(limit={limit}) completou em {sw.ElapsedMilliseconds}ms");
        }
    }

    public async Task<long?> GetTotalCountAsync(CancellationToken cancellationToken = default)
    {
        var url = $"{_baseUrl}?resultsPerPage=1";
        LogInfo($"GetTotalCountAsync: {url}");
        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.UserAgent.ParseAdd(USER_AGENT);
            using var resp = await _httpClient.SendAsync(req, cancellationToken);
            if (!resp.IsSuccessStatusCode || !IsJson(resp)) return null;
            var nvdResponse = await ReadJsonAsync<omama_cli.Models.NVD.NvdResponse>(resp, cancellationToken);
            return nvdResponse?.TotalResults;
        }
        catch (Exception ex)
        {
            LogInfo($"GetTotalCountAsync ERRO: {ex.GetType().Name} - {ex.Message}");
            return null;
        }
    }
}