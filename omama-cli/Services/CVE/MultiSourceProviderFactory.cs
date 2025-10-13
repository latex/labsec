using Microsoft.Extensions.Options;

namespace omama_cli.Services.CVE;

public class MultiSourceProviderFactory
{
    private readonly IOptions<CveProviderOptions> _options;

    public MultiSourceProviderFactory(IOptions<CveProviderOptions> options)
    {
        _options = options;
    }

    public IReadOnlyList<NamedCveProvider> CreateNamed()
    {
        var http = new HttpClient();
        var configs = SourceConfigLoader.LoadDefault();
        var loader = new CveSourceLoader();
        return loader.LoadSources(http, configs);
    }
}
