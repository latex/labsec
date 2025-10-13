using NSubstitute;

namespace omama_cli.Tests;

public class CompositeCveProviderTests
{
    [Fact]
    public async Task GetCveByIdAsync_MultipleSources_ReturnsMergedResults()
    {
        // Arrange
        var cveId = "CVE-2023-1234";
        var provider1 = Substitute.For<ICveDataProvider>();
        var provider2 = Substitute.For<ICveDataProvider>();

        var cve1 = new Models.CVE 
        { 
            Id = cveId,
            Description = "Description from source 1",
            Score = 7.5,
            Severity = "HIGH",
            References = new List<string> { "ref1" }
        };

        var cve2 = new Models.CVE 
        { 
            Id = cveId,
            Description = "Description from source 2",
            Score = 7.5,
            Severity = "HIGH",
            References = new List<string> { "ref2" }
        };

        provider1.GetCveByIdAsync(cveId).Returns(cve1);
        provider2.GetCveByIdAsync(cveId).Returns(cve2);

        var compositeProvider = new CompositeCveProvider(new[] { provider1, provider2 });

        // Act
        var result = await compositeProvider.GetCveByIdAsync(cveId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(cveId, result.Id);
        Assert.Contains("Description from source 1", result.Description);
        Assert.Contains("Description from source 2", result.Description);
        Assert.Equal(7.5, result.Score);
        Assert.Equal("HIGH", result.Severity);
        Assert.Equal(2, result.References.Count);
        Assert.Contains("ref1", result.References);
        Assert.Contains("ref2", result.References);
    }

    [Fact]
    public async Task SearchCvesAsync_MultipleSources_ReturnsMergedResults()
    {
        // Arrange
        var keyword = "test";
        var provider1 = Substitute.For<ICveDataProvider>();
        var provider2 = Substitute.For<ICveDataProvider>();

        var cves1 = new[]
        {
            new Models.CVE 
            { 
                Id = "CVE-2023-0001",
                Description = "Test vuln 1",
                Score = 7.5,
                References = new List<string> { "ref1" }
            }
        };

        var cves2 = new[]
        {
            new Models.CVE 
            { 
                Id = "CVE-2023-0001", // Mesmo ID para testar merge
                Description = "Test vuln 1 additional info",
                Score = 7.5,
                References = new List<string> { "ref2" }
            },
            new Models.CVE 
            { 
                Id = "CVE-2023-0002",
                Description = "Test vuln 2",
                Score = 5.5,
                References = new List<string> { "ref3" }
            }
        };

        provider1.SearchCvesAsync(keyword).Returns(cves1);
        provider2.SearchCvesAsync(keyword).Returns(cves2);

        var compositeProvider = new CompositeCveProvider(new[] { provider1, provider2 });

        // Act
        var results = await compositeProvider.SearchCvesAsync(keyword);
        var resultList = results.ToList();

        // Assert
        Assert.Equal(2, resultList.Count); // Deve ter apenas 2 CVEs únicos
        var mergedCve = resultList.First(c => c.Id == "CVE-2023-0001");
        Assert.Contains("Test vuln 1", mergedCve.Description);
        Assert.Contains("additional info", mergedCve.Description);
        Assert.Equal(2, mergedCve.References.Count);
    }
}