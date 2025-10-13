namespace omama_cli.Services.CVE;

public class CveProviderOptions
{
    public bool EnableNvd { get; set; } = true;
    public bool EnableCircl { get; set; } = true;
    
    public string NvdBaseUrl { get; set; } = "https://services.nvd.nist.gov/rest/json/cves/2.0";
    public string CirclBaseUrl { get; set; } = "https://api.circl.lu/v1/cve";
    
    public int CacheTimeInMinutes { get; set; } = 30;
    public int MaxParallelProcessing { get; set; } = 4; // Número de threads para processamento paralelo
}