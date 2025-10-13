using omama_cli.Models;
using System.Net;
using System.Net.Http;

namespace omama_cli.Services.Security;

public class DastScanner : ISecurityScanner
{
    private readonly HttpClient _httpClient;

    public ScanType ScanType => ScanType.DAST;

    public DastScanner(HttpClient? httpClient = null)
    {
        _httpClient = httpClient ?? new HttpClient(new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (_, _, _, _) => true, // For testing
            AllowAutoRedirect = false
        });
        _httpClient.Timeout = TimeSpan.FromSeconds(10);
    }

    public async Task<SecurityScanResult> ScanAsync(string target, CancellationToken ct = default)
    {
        var result = new SecurityScanResult
        {
            Type = ScanType.DAST,
            ScanDate = DateTime.UtcNow,
            Target = target,
            Findings = new()
        };

        if (!Uri.TryCreate(target, UriKind.Absolute, out var uri))
        {
            result.Findings.Add(new SecurityFinding
            {
                Id = $"DAST-INVALID-{Guid.NewGuid():N}",
                Title = "Target Inválido",
                Description = "O target fornecido não é uma URL válida.",
                Severity = SeverityLevel.Info,
                Location = target,
                Recommendations = new List<string> { "Forneça uma URL válida (ex: https://example.com)" }
            });
            return result;
        }

        // Check SSL/TLS
        if (uri.Scheme == "http")
        {
            result.Findings.Add(new SecurityFinding
            {
                Id = $"DAST-HTTP-{Guid.NewGuid():N}",
                Title = "Conexão Não Criptografada",
                Description = "O site usa HTTP ao invés de HTTPS.",
                Severity = SeverityLevel.High,
                Location = target,
                Recommendations = new List<string>
                {
                    "Configure HTTPS no servidor",
                    "Redirecione todo tráfego HTTP para HTTPS",
                    "Implemente HSTS (HTTP Strict Transport Security)"
                }
            });
        }

        try
        {
            // Check for common security headers
            var response = await _httpClient.GetAsync(uri, ct);
            var headers = response.Headers;

            if (!headers.Contains("X-Content-Type-Options"))
            {
                result.Findings.Add(new SecurityFinding
                {
                    Id = $"DAST-HEADER-XCT-{Guid.NewGuid():N}",
                    Title = "Header de Segurança Ausente: X-Content-Type-Options",
                    Description = "O header X-Content-Type-Options não está configurado.",
                    Severity = SeverityLevel.Medium,
                    Location = target,
                    Recommendations = new List<string>
                    {
                        "Adicione o header: X-Content-Type-Options: nosniff",
                        "Isso previne MIME type sniffing"
                    }
                });
            }

            if (!headers.Contains("X-Frame-Options") && !headers.Contains("Content-Security-Policy"))
            {
                result.Findings.Add(new SecurityFinding
                {
                    Id = $"DAST-HEADER-XFO-{Guid.NewGuid():N}",
                    Title = "Header de Segurança Ausente: X-Frame-Options",
                    Description = "O header X-Frame-Options não está configurado.",
                    Severity = SeverityLevel.Medium,
                    Location = target,
                    Recommendations = new List<string>
                    {
                        "Adicione o header: X-Frame-Options: DENY ou SAMEORIGIN",
                        "Ou configure frame-ancestors no Content-Security-Policy",
                        "Isso previne clickjacking attacks"
                    }
                });
            }

            if (!headers.Contains("Strict-Transport-Security") && uri.Scheme == "https")
            {
                result.Findings.Add(new SecurityFinding
                {
                    Id = $"DAST-HEADER-HSTS-{Guid.NewGuid():N}",
                    Title = "Header de Segurança Ausente: HSTS",
                    Description = "O header Strict-Transport-Security não está configurado.",
                    Severity = SeverityLevel.Medium,
                    Location = target,
                    Recommendations = new List<string>
                    {
                        "Adicione o header: Strict-Transport-Security: max-age=31536000; includeSubDomains",
                        "Isso força o uso de HTTPS"
                    }
                });
            }

            if (!headers.Contains("Content-Security-Policy"))
            {
                result.Findings.Add(new SecurityFinding
                {
                    Id = $"DAST-HEADER-CSP-{Guid.NewGuid():N}",
                    Title = "Header de Segurança Ausente: Content-Security-Policy",
                    Description = "O header Content-Security-Policy não está configurado.",
                    Severity = SeverityLevel.Medium,
                    Location = target,
                    Recommendations = new List<string>
                    {
                        "Configure uma política CSP apropriada",
                        "Isso ajuda a prevenir XSS e outras injeções de código"
                    }
                });
            }

            if (!headers.Contains("X-XSS-Protection"))
            {
                result.Findings.Add(new SecurityFinding
                {
                    Id = $"DAST-HEADER-XXP-{Guid.NewGuid():N}",
                    Title = "Header de Segurança Ausente: X-XSS-Protection",
                    Description = "O header X-XSS-Protection não está configurado.",
                    Severity = SeverityLevel.Low,
                    Location = target,
                    Recommendations = new List<string>
                    {
                        "Adicione o header: X-XSS-Protection: 1; mode=block",
                        "Nota: CSP é mais efetivo, mas este header oferece proteção adicional"
                    }
                });
            }

            // Check for server information disclosure
            if (headers.Contains("Server") || headers.Contains("X-Powered-By"))
            {
                result.Findings.Add(new SecurityFinding
                {
                    Id = $"DAST-DISCLOSURE-{Guid.NewGuid():N}",
                    Title = "Divulgação de Informações do Servidor",
                    Description = "O servidor está divulgando informações de versão.",
                    Severity = SeverityLevel.Low,
                    Location = target,
                    Recommendations = new List<string>
                    {
                        "Remova ou oculte headers Server e X-Powered-By",
                        "Isso dificulta ataques direcionados"
                    }
                });
            }
        }
        catch (HttpRequestException)
        {
            result.Findings.Add(new SecurityFinding
            {
                Id = $"DAST-ERROR-{Guid.NewGuid():N}",
                Title = "Erro ao Acessar Target",
                Description = "Não foi possível conectar ao target.",
                Severity = SeverityLevel.Info,
                Location = target,
                Recommendations = new List<string>
                {
                    "Verifique se o target está acessível",
                    "Verifique configurações de firewall e rede"
                }
            });
        }
        catch (TaskCanceledException)
        {
            result.Findings.Add(new SecurityFinding
            {
                Id = $"DAST-TIMEOUT-{Guid.NewGuid():N}",
                Title = "Timeout ao Acessar Target",
                Description = "O target não respondeu a tempo.",
                Severity = SeverityLevel.Info,
                Location = target,
                Recommendations = new List<string>
                {
                    "Verifique a disponibilidade do target",
                    "O servidor pode estar lento ou sobrecarregado"
                }
            });
        }

        return result;
    }
}
