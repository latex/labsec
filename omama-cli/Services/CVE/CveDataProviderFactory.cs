using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;

namespace omama_cli.Services.CVE;

public class CveDataProviderFactory
{
    private readonly IOptions<CveProviderOptions> _options;

    public CveDataProviderFactory(IOptions<CveProviderOptions> options)
    {
        _options = options;
    }

    public (ICveDataProvider Provider, ParallelCveProcessor Processor) Create()
    {
        var httpClient = new HttpClient();
        var providers = new List<ICveDataProvider>();

        // Adiciona provedores conforme configuração
        if (_options.Value.EnableNvd)
        {
            providers.Add(new NvdCveProvider(httpClient, _options.Value.NvdBaseUrl));
        }

        if (_options.Value.EnableCircl)
        {
            providers.Add(new CircleCveProvider(httpClient, _options.Value.CirclBaseUrl));
        }

        if (!providers.Any())
        {
            throw new InvalidOperationException("No CVE providers are enabled");
        }

        // Cria o provedor composto
        ICveDataProvider baseProvider = providers.Count == 1 
            ? providers[0] 
            : new CompositeCveProvider(providers);

    // Aplica decorators
    var loggerFactory = LoggerFactory.Create(builder => {});
    var logger = loggerFactory.CreateLogger<CveProviderLoggingDecorator>();
    baseProvider = new CveProviderLoggingDecorator(baseProvider, logger);
        baseProvider = new CveProviderCacheDecorator(baseProvider, TimeSpan.FromMinutes(_options.Value.CacheTimeInMinutes));

        // Cria o processador paralelo
        var processor = new ParallelCveProcessor(_options.Value.MaxParallelProcessing);

        return (baseProvider, processor);
    }
}