using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CVEModel = omama_cli.Models.CVE;

namespace omama_cli.Services.CVE;

public class ParallelCveProcessor
{
    private readonly SemaphoreSlim _throttler;
    private readonly ConcurrentDictionary<string, CVEModel> _processedCves;

    public ParallelCveProcessor(int maxDegreeOfParallelism)
    {
        _throttler = new SemaphoreSlim(Math.Max(1, maxDegreeOfParallelism));
        _processedCves = new ConcurrentDictionary<string, CVEModel>();
    }

    public async Task<IEnumerable<CVEModel>> ProcessCvesAsync(
        string keyword,
        ICveDataProvider provider,
        CancellationToken cancellationToken = default)
    {
        _processedCves.Clear();

        try
        {
            var cves = await provider.SearchCvesAsync(keyword);
            var tasks = new List<Task>();

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
            await Console.Error.WriteLineAsync($"Erro ao processar CVE {cveId}: {ex.Message}");
        }
    }

    private static async Task EnrichCveDataAsync(CVEModel cve, CancellationToken cancellationToken)
    {
        await Task.Run(async () =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            cve.LastModifiedDate = DateTime.UtcNow;
            await Task.Delay(50, cancellationToken);
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