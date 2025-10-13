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
        var list = new List<NamedCveProvider>();

        var http = new HttpClient();
        if (_options.Value.EnableNvd)
            list.Add(new NamedCveProvider("NVD", new NvdCveProvider(http, _options.Value.NvdBaseUrl)));
        if (_options.Value.EnableCircl)
            list.Add(new NamedCveProvider("CIRCL", new CircleCveProvider(http, _options.Value.CirclBaseUrl)));

        return list;
    }
}
