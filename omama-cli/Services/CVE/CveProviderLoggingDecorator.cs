using Microsoft.Extensions.Logging;

namespace omama_cli.Services.CVE;

public class CveProviderLoggingDecorator : ICveDataProvider
{
    private readonly ICveDataProvider _inner;
    private readonly ILogger<CveProviderLoggingDecorator> _logger;

    public CveProviderLoggingDecorator(ICveDataProvider inner, ILogger<CveProviderLoggingDecorator> logger)
    {
        _inner = inner;
        _logger = logger;
    }

    public async Task<Models.CVE?> GetCveByIdAsync(string cveId)
    {
        try
        {
            _logger.LogInformation("Buscando CVE com ID: {CveId}", cveId);
            var result = await _inner.GetCveByIdAsync(cveId);
            _logger.LogInformation("CVE {CveId} encontrado: {Found}", cveId, result != null);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar CVE {CveId}", cveId);
            throw;
        }
    }

    public async Task<IEnumerable<Models.CVE>> SearchCvesAsync(string keyword)
    {
        try
        {
            _logger.LogInformation("Buscando CVEs com palavra-chave: {Keyword}", keyword);
            var results = await _inner.SearchCvesAsync(keyword);
            _logger.LogInformation("Encontrados {Count} CVEs para a palavra-chave {Keyword}", 
                results.Count(), keyword);
            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar CVEs com palavra-chave {Keyword}", keyword);
            throw;
        }
    }

    public async Task<IEnumerable<Models.CVE>> GetLatestCvesAsync(int limit = 10)
    {
        try
        {
            _logger.LogInformation("Buscando {Limit} CVEs mais recentes", limit);
            var results = await _inner.GetLatestCvesAsync(limit);
            _logger.LogInformation("Encontrados {Count} CVEs recentes", results.Count());
            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar CVEs mais recentes");
            throw;
        }
    }
}