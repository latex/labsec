namespace omama_cli.Services.CVE;

public interface ICveDataProvider
{
    Task<Models.CVE?> GetCveByIdAsync(string cveId);
    Task<IEnumerable<Models.CVE>> SearchCvesAsync(string keyword);
    Task<IEnumerable<Models.CVE>> GetLatestCvesAsync(int limit = 10);
}