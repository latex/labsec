namespace omama_cli.Models;

public enum ScanType
{
    SAST,    // Static Application Security Testing
    DAST,    // Dynamic Application Security Testing
    CVE      // CVE Verification
}

public enum SeverityLevel
{
    Critical,
    High,
    Medium,
    Low,
    Info
}

public class SecurityFinding
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public SeverityLevel Severity { get; set; }
    public string Location { get; set; } = string.Empty;
    public List<string> Recommendations { get; set; } = new();
    public string? CveId { get; set; }
}

public class SecurityScanResult
{
    public ScanType Type { get; set; }
    public DateTime ScanDate { get; set; }
    public string Target { get; set; } = string.Empty;
    public List<SecurityFinding> Findings { get; set; } = new();
    public int CriticalCount => Findings.Count(f => f.Severity == SeverityLevel.Critical);
    public int HighCount => Findings.Count(f => f.Severity == SeverityLevel.High);
    public int MediumCount => Findings.Count(f => f.Severity == SeverityLevel.Medium);
    public int LowCount => Findings.Count(f => f.Severity == SeverityLevel.Low);
    public int InfoCount => Findings.Count(f => f.Severity == SeverityLevel.Info);
    public int TotalCount => Findings.Count;
}
