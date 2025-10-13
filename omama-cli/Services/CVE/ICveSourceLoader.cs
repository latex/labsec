using System.Collections.Generic;
using System.Net.Http;

namespace omama_cli.Services.CVE;

public interface ICveSourceLoader
{
    /// Returns a list of NamedCveProvider for all sources in the config, using real or stub providers.
    IReadOnlyList<NamedCveProvider> LoadSources(HttpClient httpClient, IEnumerable<SourceConfig> configs);
}
