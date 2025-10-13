using System.Collections.Generic;
using System.Threading.Tasks;
using omama_cli.Models;

namespace omama_cli.Services.CVE;

public class StubCveProvider : ICveDataProvider
{
    private readonly string _name;
    public StubCveProvider(string name) { _name = name; }

    public Task<omama_cli.Models.CVE?> GetCveByIdAsync(string cveId) => Task.FromResult<omama_cli.Models.CVE?>(null);
    public Task<IEnumerable<omama_cli.Models.CVE>> SearchCvesAsync(string keyword) => Task.FromResult<IEnumerable<omama_cli.Models.CVE>>(new List<omama_cli.Models.CVE>());
    public Task<IEnumerable<omama_cli.Models.CVE>> GetLatestCvesAsync(int limit = 10) => Task.FromResult<IEnumerable<omama_cli.Models.CVE>>(new List<omama_cli.Models.CVE>());
}