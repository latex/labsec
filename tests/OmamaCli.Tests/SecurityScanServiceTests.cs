using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using omama_cli.Services.Security;
using omama_cli.Models;

namespace OmamaCli.Tests;

public class SecurityScanServiceTests
{
    [Fact]
    public async Task SastScanner_DetectsSqlInjection()
    {
        // Arrange
        var scanner = new SastScanner();
        var testFile = Path.Combine(Path.GetTempPath(), "test_sql.cs");
        await File.WriteAllTextAsync(testFile, @"
            public void Query(string input) {
                string query = ""SELECT * FROM users WHERE id = "" + input;
                ExecuteQuery(query);
            }
        ");

        try
        {
            // Act
            var result = await scanner.ScanAsync(testFile);

            // Assert
            Assert.NotEmpty(result.Findings);
            Assert.Contains(result.Findings, f => f.Title.Contains("SQL Injection"));
        }
        finally
        {
            if (File.Exists(testFile))
                File.Delete(testFile);
        }
    }

    [Fact]
    public async Task SastScanner_DetectsWeakCryptography()
    {
        // Arrange
        var scanner = new SastScanner();
        var testFile = Path.Combine(Path.GetTempPath(), "test_crypto.cs");
        await File.WriteAllTextAsync(testFile, @"
            public void Hash() {
                var hasher = System.Security.Cryptography.MD5.Create();
            }
        ");

        try
        {
            // Act
            var result = await scanner.ScanAsync(testFile);

            // Assert
            Assert.NotEmpty(result.Findings);
            Assert.Contains(result.Findings, f => f.Title.Contains("Criptografia Fraca"));
        }
        finally
        {
            if (File.Exists(testFile))
                File.Delete(testFile);
        }
    }

    [Fact]
    public async Task DastScanner_DetectsHttpUsage()
    {
        // Arrange
        var scanner = new DastScanner();
        var target = "http://example.com";

        // Act
        var result = await scanner.ScanAsync(target);

        // Assert
        Assert.NotEmpty(result.Findings);
        Assert.Contains(result.Findings, f => f.Title.Contains("Não Criptografada") || f.Severity == SeverityLevel.High);
    }

    [Fact]
    public async Task DastScanner_ValidatesTarget()
    {
        // Arrange
        var scanner = new DastScanner();
        var invalidTarget = "not-a-valid-url";

        // Act
        var result = await scanner.ScanAsync(invalidTarget);

        // Assert
        Assert.NotEmpty(result.Findings);
        Assert.Contains(result.Findings, f => f.Title.Contains("Inválido"));
    }

    [Fact]
    public async Task SecurityScanService_GeneratesReport()
    {
        // Arrange
        var scanners = new List<ISecurityScanner> { new SastScanner() };
        var service = new SecurityScanService(scanners);
        var testFile = Path.Combine(Path.GetTempPath(), "test_report.cs");
        await File.WriteAllTextAsync(testFile, @"
            public void Vulnerable() {
                var md5 = System.Security.Cryptography.MD5.Create();
            }
        ");

        try
        {
            // Act
            var results = await service.RunAllScansAsync(testFile);
            var report = service.GenerateRecommendationReport(results);

            // Assert
            Assert.NotEmpty(results);
            Assert.True(report.TotalFindings > 0);
            Assert.NotEmpty(report.PrioritizedRecommendations);
        }
        finally
        {
            if (File.Exists(testFile))
                File.Delete(testFile);
        }
    }

    [Fact]
    public async Task SecurityScanService_RunsSpecificScanType()
    {
        // Arrange
        var scanners = new List<ISecurityScanner> { new SastScanner(), new DastScanner() };
        var service = new SecurityScanService(scanners);
        
        // Act
        var result = await service.RunScanAsync(ScanType.DAST, "http://test.com");

        // Assert
        Assert.Equal(ScanType.DAST, result.Type);
        Assert.NotEmpty(result.Findings);
    }

    [Fact]
    public void SecurityScanResult_CountsCorrectly()
    {
        // Arrange
        var result = new SecurityScanResult
        {
            Type = ScanType.SAST,
            Target = "test",
            Findings = new List<SecurityFinding>
            {
                new SecurityFinding { Severity = SeverityLevel.Critical },
                new SecurityFinding { Severity = SeverityLevel.High },
                new SecurityFinding { Severity = SeverityLevel.High },
                new SecurityFinding { Severity = SeverityLevel.Medium },
            }
        };

        // Assert
        Assert.Equal(4, result.TotalCount);
        Assert.Equal(1, result.CriticalCount);
        Assert.Equal(2, result.HighCount);
        Assert.Equal(1, result.MediumCount);
        Assert.Equal(0, result.LowCount);
    }
}
