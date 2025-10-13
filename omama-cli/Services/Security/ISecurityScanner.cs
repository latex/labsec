namespace omama_cli.Services.Security;

public interface ISecurityScanner
{
    Task<Models.SecurityScanResult> ScanAsync(string target, CancellationToken ct = default);
    Models.ScanType ScanType { get; }
}
