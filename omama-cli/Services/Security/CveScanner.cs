using omama_cli.Models;
using omama_cli.Services.CVE;

namespace omama_cli.Services.Security;

public class CveScanner : ISecurityScanner
{
    private readonly ICveDataProvider _cveProvider;
    private readonly string _cacheDir;

    public ScanType ScanType => ScanType.CVE;

    public CveScanner(ICveDataProvider cveProvider, string cacheDir)
    {
        _cveProvider = cveProvider;
        _cacheDir = cacheDir;
    }

    public async Task<SecurityScanResult> ScanAsync(string target, CancellationToken ct = default)
    {
        var result = new SecurityScanResult
        {
            Type = ScanType.CVE,
            ScanDate = DateTime.UtcNow,
            Target = target,
            Findings = new()
        };

        // Skip CVE search for URLs or file paths - CVE is meant for technology keywords
        if (Uri.TryCreate(target, UriKind.Absolute, out _) || 
            File.Exists(target) || 
            Directory.Exists(target))
        {
            result.Findings.Add(new SecurityFinding
            {
                Id = $"CVE-SKIP-{Guid.NewGuid():N}",
                Title = "CVE Scan Não Aplicável",
                Description = "CVE scan é aplicável apenas para nomes de tecnologias ou produtos (ex: 'spring', 'log4j', 'nodejs').",
                Severity = SeverityLevel.Info,
                Location = target,
                Recommendations = new List<string>
                {
                    "Use CVE scan com nomes de tecnologias: omama-cli scan cve --target 'spring framework'",
                    "Para arquivos use SAST scan",
                    "Para URLs use DAST scan"
                }
            });
            return result;
        }

        // Search for CVEs related to the target (could be a technology, package, or keyword)
        IEnumerable<Models.CVE> cves;
        try
        {
            cves = await _cveProvider.SearchCvesAsync(target);
        }
        catch (Exception ex)
        {
            result.Findings.Add(new SecurityFinding
            {
                Id = $"CVE-ERROR-{Guid.NewGuid():N}",
                Title = "Erro ao Buscar CVEs",
                Description = $"Erro ao buscar CVEs: {ex.Message}",
                Severity = SeverityLevel.Info,
                Location = target,
                Recommendations = new List<string>
                {
                    "Verifique sua conexão com a internet",
                    "Tente novamente mais tarde"
                }
            });
            return result;
        }
        
        foreach (var cve in cves)
        {
            var severity = MapCveSeverityToSecuritySeverity(cve.Severity, cve.Score);
            
            var recommendations = new List<string>
            {
                $"CVE Score: {cve.Score}",
                $"Published: {cve.PublishedDate:yyyy-MM-dd}",
                "Verifique se sua versão está afetada"
            };

            if (cve.References.Any())
            {
                recommendations.Add("Referências:");
                recommendations.AddRange(cve.References.Take(3));
            }

            // Add mitigation suggestions based on severity
            if (severity == SeverityLevel.Critical || severity == SeverityLevel.High)
            {
                recommendations.Insert(0, "AÇÃO URGENTE: Atualize ou aplique patch imediatamente");
            }
            else
            {
                recommendations.Insert(0, "Planeje atualização ou mitigação");
            }

            result.Findings.Add(new SecurityFinding
            {
                Id = cve.Id,
                Title = $"CVE Detectado: {cve.Id}",
                Description = cve.Description,
                Severity = severity,
                Location = target,
                Recommendations = recommendations,
                CveId = cve.Id
            });
        }

        // If no CVEs found, add informational finding
        if (!result.Findings.Any())
        {
            result.Findings.Add(new SecurityFinding
            {
                Id = $"CVE-NONE-{Guid.NewGuid():N}",
                Title = "Nenhum CVE Encontrado",
                Description = $"Nenhuma vulnerabilidade CVE foi encontrada para '{target}'.",
                Severity = SeverityLevel.Info,
                Location = target,
                Recommendations = new List<string>
                {
                    "Continue monitorando novas CVEs regularmente",
                    "Mantenha suas dependências atualizadas"
                }
            });
        }

        return result;
    }

    private static SeverityLevel MapCveSeverityToSecuritySeverity(string cveSeverity, double score)
    {
        // Map based on CVSS score
        if (score >= 9.0) return SeverityLevel.Critical;
        if (score >= 7.0) return SeverityLevel.High;
        if (score >= 4.0) return SeverityLevel.Medium;
        if (score > 0.0) return SeverityLevel.Low;

        // Fallback to text severity
        return cveSeverity.ToUpperInvariant() switch
        {
            "CRITICAL" => SeverityLevel.Critical,
            "HIGH" => SeverityLevel.High,
            "MEDIUM" => SeverityLevel.Medium,
            "LOW" => SeverityLevel.Low,
            _ => SeverityLevel.Info
        };
    }
}
