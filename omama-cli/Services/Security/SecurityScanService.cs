using omama_cli.Models;

namespace omama_cli.Services.Security;

public class SecurityScanService
{
    private readonly List<ISecurityScanner> _scanners;

    public SecurityScanService(IEnumerable<ISecurityScanner> scanners)
    {
        _scanners = scanners.ToList();
    }

    public async Task<List<SecurityScanResult>> RunAllScansAsync(string target, CancellationToken ct = default)
    {
        var results = new List<SecurityScanResult>();

        foreach (var scanner in _scanners)
        {
            try
            {
                var result = await scanner.ScanAsync(target, ct);
                results.Add(result);
            }
            catch (Exception ex)
            {
                // Add error result
                results.Add(new SecurityScanResult
                {
                    Type = scanner.ScanType,
                    ScanDate = DateTime.UtcNow,
                    Target = target,
                    Findings = new List<SecurityFinding>
                    {
                        new SecurityFinding
                        {
                            Id = $"ERROR-{scanner.ScanType}-{Guid.NewGuid():N}",
                            Title = $"Erro ao executar scan {scanner.ScanType}",
                            Description = ex.Message,
                            Severity = SeverityLevel.Info,
                            Location = target,
                            Recommendations = new List<string> { "Verifique os logs para mais detalhes" }
                        }
                    }
                });
            }
        }

        return results;
    }

    public async Task<SecurityScanResult> RunScanAsync(ScanType scanType, string target, CancellationToken ct = default)
    {
        var scanner = _scanners.FirstOrDefault(s => s.ScanType == scanType);
        
        if (scanner == null)
        {
            return new SecurityScanResult
            {
                Type = scanType,
                ScanDate = DateTime.UtcNow,
                Target = target,
                Findings = new List<SecurityFinding>
                {
                    new SecurityFinding
                    {
                        Id = $"ERROR-{scanType}-{Guid.NewGuid():N}",
                        Title = $"Scanner {scanType} não disponível",
                        Description = "O tipo de scanner solicitado não está configurado.",
                        Severity = SeverityLevel.Info,
                        Location = target,
                        Recommendations = new List<string> { "Verifique a configuração do serviço" }
                    }
                }
            };
        }

        return await scanner.ScanAsync(target, ct);
    }

    public SecurityRecommendationReport GenerateRecommendationReport(List<SecurityScanResult> scanResults)
    {
        var allFindings = scanResults.SelectMany(r => r.Findings).ToList();
        
        var criticalFindings = allFindings.Where(f => f.Severity == SeverityLevel.Critical).ToList();
        var highFindings = allFindings.Where(f => f.Severity == SeverityLevel.High).ToList();
        var mediumFindings = allFindings.Where(f => f.Severity == SeverityLevel.Medium).ToList();

        var prioritizedRecommendations = new List<string>();

        // Critical recommendations first
        if (criticalFindings.Any())
        {
            prioritizedRecommendations.Add("=== CRÍTICO - Ação Imediata Necessária ===");
            foreach (var finding in criticalFindings)
            {
                prioritizedRecommendations.Add($"\n[{finding.Title}]");
                prioritizedRecommendations.AddRange(finding.Recommendations.Select(r => $"  • {r}"));
            }
        }

        // High severity recommendations
        if (highFindings.Any())
        {
            prioritizedRecommendations.Add("\n=== ALTA - Resolver em até 7 dias ===");
            foreach (var finding in highFindings)
            {
                prioritizedRecommendations.Add($"\n[{finding.Title}]");
                prioritizedRecommendations.AddRange(finding.Recommendations.Select(r => $"  • {r}"));
            }
        }

        // Medium severity recommendations
        if (mediumFindings.Any())
        {
            prioritizedRecommendations.Add("\n=== MÉDIA - Resolver em até 30 dias ===");
            foreach (var finding in mediumFindings.Take(5))
            {
                prioritizedRecommendations.Add($"\n[{finding.Title}]");
                prioritizedRecommendations.AddRange(finding.Recommendations.Select(r => $"  • {r}"));
            }
            if (mediumFindings.Count > 5)
            {
                prioritizedRecommendations.Add($"\n... e mais {mediumFindings.Count - 5} problemas médios");
            }
        }

        return new SecurityRecommendationReport
        {
            GeneratedAt = DateTime.UtcNow,
            TotalFindings = allFindings.Count,
            CriticalCount = criticalFindings.Count,
            HighCount = highFindings.Count,
            MediumCount = mediumFindings.Count,
            LowCount = allFindings.Count(f => f.Severity == SeverityLevel.Low),
            PrioritizedRecommendations = prioritizedRecommendations,
            ScanResults = scanResults
        };
    }
}

public class SecurityRecommendationReport
{
    public DateTime GeneratedAt { get; set; }
    public int TotalFindings { get; set; }
    public int CriticalCount { get; set; }
    public int HighCount { get; set; }
    public int MediumCount { get; set; }
    public int LowCount { get; set; }
    public List<string> PrioritizedRecommendations { get; set; } = new();
    public List<SecurityScanResult> ScanResults { get; set; } = new();
}
