using System.Collections.Generic;
using System.Net.Http;
using omama_cli.Models;

namespace omama_cli.Services.CVE;

public class CveSourceLoader : ICveSourceLoader
{
    public IReadOnlyList<NamedCveProvider> LoadSources(HttpClient httpClient, IEnumerable<SourceConfig> configs)
    {
        var list = new List<NamedCveProvider>();
        foreach (var cfg in configs)
        {
            if (!cfg.Enabled) continue;
            switch (cfg.Name.ToUpperInvariant())
            {
                case "NVD":
                    list.Add(new NamedCveProvider(cfg.Name, new NvdCveProvider(httpClient, cfg.Url)));
                    break;
                case "CIRCL":
                    list.Add(new NamedCveProvider(cfg.Name, new CircleCveProvider(httpClient, cfg.Url)));
                    break;
                case "CVE.ORG":
                    list.Add(new NamedCveProvider(cfg.Name, new CveOrgCveProvider(httpClient, cfg.Url)));
                    break;
                case "CVEDETAILS":
                    list.Add(new NamedCveProvider(cfg.Name, new CveDetailsCveProvider(httpClient, cfg.Url)));
                    break;
                case "VULNERS":
                    list.Add(new NamedCveProvider(cfg.Name, new VulnersCveProvider(httpClient, cfg.Url)));
                    break;
                case "VULDB":
                    list.Add(new NamedCveProvider(cfg.Name, new VuldbCveProvider(httpClient, cfg.Url)));
                    break;
                case "WIZ":
                    list.Add(new NamedCveProvider(cfg.Name, new WizCveProvider(httpClient, cfg.Url)));
                    break;
                case "EXPLOIT-DB":
                    list.Add(new NamedCveProvider(cfg.Name, new ExploitDbCveProvider(httpClient, cfg.Url)));
                    break;
                case "IVANTI":
                    list.Add(new NamedCveProvider(cfg.Name, new IvantiCveProvider(httpClient, cfg.Url)));
                    break;
                default:
                    list.Add(new NamedCveProvider(cfg.Name, new StubCveProvider(cfg.Name)));
                    break;
            }
        }
        return list;
    }
}