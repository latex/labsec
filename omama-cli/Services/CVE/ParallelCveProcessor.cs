using System.Collections.Concurrent;

namespace omama_cli.Services.CVE;

public class ParallelCveProcessor
{
    private readonly int _maxDegreeOfParallelism;
    private readonly SemaphoreSlim _throttler;
    private readonly ConcurrentDictionary<string, Models.CVE> _processedCves;

    public ParallelCveProcessor(int maxDegreeOfParallelism)
    {
        _maxDegreeOfParallelism = maxDegreeOfParallelism;
        _throttler = new SemaphoreSlim(maxDegreeOfParallelism);
        _processedCves = new ConcurrentDictionary<string, Models.CVE>();
    }

    public async Task<IEnumerable<Models.CVE>> ProcessCvesAsync(
        string keyword, 
        ICveDataProvider provider,
        CancellationToken cancellationToken = default)
    {
        _processedCves.Clear();

        try
        {
            // Busca os CVEs iniciais
            var cves = await provider.SearchCvesAsync(keyword);
            var tasks = new List<Task>();

            // Processa cada CVE em paralelo
            foreach (var cve in cves)
            {
                cancellationToken.ThrowIfCancellationRequested();

                tasks.Add(ProcessCveDetailAsync(cve.Id, provider, cancellationToken));
            }

            await Task.WhenAll(tasks);

            return _processedCves.Values;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new CveProcessingException("Erro ao processar CVEs em paralelo", ex);
        }
    }

    private async Task ProcessCveDetailAsync(string cveId, ICveDataProvider provider, CancellationToken cancellationToken)
    {
        try
        {
            await _throttler.WaitAsync(cancellationToken);

            try
            {
                var detailedCve = await provider.GetCveByIdAsync(cveId);
                if (detailedCve != null)
                {
                    await EnrichCveDataAsync(detailedCve, cancellationToken);
                    _processedCves.TryAdd(detailedCve.Id, detailedCve);
                }
            }
            finally
            {
                _throttler.Release();
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            // Log erro mas continua processamento
            Console.Error.WriteLine($"Erro ao processar CVE {cveId}: {ex.Message}");
        }
    }

    private async Task EnrichCveDataAsync(Models.CVE cve, CancellationToken cancellationToken)
    {
        // Simula enriquecimento de dados assíncrono
        // Aqui você pode adicionar chamadas para outras APIs ou fontes de dados
        await Task.Run(async () =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            // Exemplo de enriquecimento: adiciona timestamp de processamento
            cve.LastModifiedDate = DateTime.UtcNow;
            
            // Simula algum processamento
            await Task.Delay(100, cancellationToken);
        }, cancellationToken);
    }
}

public class CveProcessingException : Exception
{
    public CveProcessingException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}