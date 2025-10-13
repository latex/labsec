using System.Collections.Generic;
namespace omama_cli.Services.CVE;

public static class SourceConfigLoader
{
    public static List<SourceConfig> LoadDefault()
    {
        var configs = new List<SourceConfig>();
        foreach (var src in CveSourceCatalog.All)
        {
            // HABILITA TODOS OS SOURCES como solicitado pelo usuário
            configs.Add(new SourceConfig(src.Name, src.Url, true));
        }
        return configs;
    }
}