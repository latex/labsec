using omama_cli.Models;

namespace omama_cli.Services.Security;

public class SastScanner : ISecurityScanner
{
    public ScanType ScanType => ScanType.SAST;

    public async Task<SecurityScanResult> ScanAsync(string target, CancellationToken ct = default)
    {
        var result = new SecurityScanResult
        {
            Type = ScanType.SAST,
            ScanDate = DateTime.UtcNow,
            Target = target,
            Findings = new()
        };

        // Simulate SAST scanning
        await Task.Delay(100, ct);

        // Check for common code patterns and vulnerabilities
        if (Directory.Exists(target))
        {
            await ScanDirectoryAsync(target, result, ct);
        }
        else if (File.Exists(target))
        {
            await ScanFileAsync(target, result, ct);
        }

        return result;
    }

    private async Task ScanDirectoryAsync(string directory, SecurityScanResult result, CancellationToken ct)
    {
        var files = Directory.GetFiles(directory, "*.*", SearchOption.AllDirectories)
            .Where(f => IsCodeFile(f))
            .ToList();

        foreach (var file in files)
        {
            ct.ThrowIfCancellationRequested();
            await ScanFileAsync(file, result, ct);
        }
    }

    private async Task ScanFileAsync(string filePath, SecurityScanResult result, CancellationToken ct)
    {
        try
        {
            var content = await File.ReadAllTextAsync(filePath, ct);
            var relativePath = Path.GetFileName(filePath);

            // Check for SQL injection vulnerabilities
            var sqlKeywords = new[] { "SELECT", "INSERT", "UPDATE", "DELETE", "DROP", "CREATE" };
            var hasSqlKeyword = sqlKeywords.Any(kw => content.Contains(kw, StringComparison.OrdinalIgnoreCase));
            
            // Look for string concatenation patterns specific to SQL queries
            // Matches patterns like: "SELECT * FROM users WHERE id = " + variable
            var sqlConcatPattern = @"(""[^""]*(?:SELECT|INSERT|UPDATE|DELETE)[^""]*""\s*\+)|('[^']*(?:SELECT|INSERT|UPDATE|DELETE)[^']*'\s*\+)";
            var hasSqlStringConcatenation = System.Text.RegularExpressions.Regex.IsMatch(content, sqlConcatPattern, 
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            
            // Look for SQL execution methods
            var sqlExecutionMethods = new[] { "ExecuteQuery", "ExecuteNonQuery", "ExecuteScalar", "ExecuteReader", ".query(", ".execute(" };
            var hasQueryMethod = sqlExecutionMethods.Any(m => content.Contains(m, StringComparison.OrdinalIgnoreCase));
            
            if (hasSqlKeyword && hasSqlStringConcatenation && hasQueryMethod)
            {
                result.Findings.Add(new SecurityFinding
                {
                    Id = $"SAST-SQL-{Guid.NewGuid():N}",
                    Title = "Possível SQL Injection",
                    Description = "Detectada concatenação de strings em query SQL. Use parâmetros preparados.",
                    Severity = SeverityLevel.High,
                    Location = relativePath,
                    Recommendations = new List<string>
                    {
                        "Use prepared statements ou parameterized queries",
                        "Nunca concatene entrada do usuário diretamente em queries SQL",
                        "Utilize ORM com proteção contra SQL injection"
                    }
                });
            }

            // Check for hardcoded credentials
            var credentialPatterns = new[] { "password=", "pwd=", "api_key=", "apikey=", "secret=", "token=" };
            foreach (var pattern in credentialPatterns)
            {
                if (content.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                {
                    result.Findings.Add(new SecurityFinding
                    {
                        Id = $"SAST-CRED-{Guid.NewGuid():N}",
                        Title = "Credenciais Hardcoded",
                        Description = "Detectadas possíveis credenciais no código-fonte.",
                        Severity = SeverityLevel.Critical,
                        Location = relativePath,
                        Recommendations = new List<string>
                        {
                            "Remova credenciais do código-fonte",
                            "Use variáveis de ambiente ou serviços de gerenciamento de secrets",
                            "Adicione o arquivo ao .gitignore se contiver secrets",
                            "Revogue e recrie as credenciais expostas"
                        }
                    });
                    break;
                }
            }

            // Check for weak cryptography
            if (content.Contains("DES") || content.Contains("MD5"))
            {
                result.Findings.Add(new SecurityFinding
                {
                    Id = $"SAST-CRYPTO-{Guid.NewGuid():N}",
                    Title = "Criptografia Fraca",
                    Description = "Detectado uso de algoritmos criptográficos fracos (DES, MD5).",
                    Severity = SeverityLevel.High,
                    Location = relativePath,
                    Recommendations = new List<string>
                    {
                        "Use AES para criptografia simétrica",
                        "Use SHA-256 ou superior para hashing",
                        "Considere usar bcrypt ou Argon2 para senhas"
                    }
                });
            }

            // Check for potential XSS
            if (content.Contains("innerHTML") || (content.Contains("Response.Write") && content.Contains("Request[")))
            {
                result.Findings.Add(new SecurityFinding
                {
                    Id = $"SAST-XSS-{Guid.NewGuid():N}",
                    Title = "Possível Cross-Site Scripting (XSS)",
                    Description = "Detectada renderização de dados não sanitizados.",
                    Severity = SeverityLevel.High,
                    Location = relativePath,
                    Recommendations = new List<string>
                    {
                        "Sempre sanitize e encode entrada de usuário",
                        "Use bibliotecas de proteção contra XSS",
                        "Implemente Content Security Policy (CSP)"
                    }
                });
            }

            // Check for insecure deserialization
            if (content.Contains("BinaryFormatter") || content.Contains("JavaScriptSerializer"))
            {
                result.Findings.Add(new SecurityFinding
                {
                    Id = $"SAST-DESER-{Guid.NewGuid():N}",
                    Title = "Deserialização Insegura",
                    Description = "Uso de desserializadores inseguros detectado.",
                    Severity = SeverityLevel.Critical,
                    Location = relativePath,
                    Recommendations = new List<string>
                    {
                        "Use System.Text.Json ao invés de BinaryFormatter",
                        "Valide tipos antes de desserializar",
                        "Não desserialize dados não confiáveis"
                    }
                });
            }
        }
        catch (Exception)
        {
            // Skip files that can't be read
        }
    }

    private static bool IsCodeFile(string filePath)
    {
        var ext = Path.GetExtension(filePath).ToLowerInvariant();
        return ext switch
        {
            ".cs" => true,
            ".js" => true,
            ".ts" => true,
            ".java" => true,
            ".py" => true,
            ".php" => true,
            ".rb" => true,
            ".go" => true,
            ".cpp" => true,
            ".c" => true,
            ".h" => true,
            _ => false
        };
    }
}
