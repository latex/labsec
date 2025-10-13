namespace omama_cli.Tests;

public class CveProviderDecoratorTests
{
    private readonly ICveDataProvider _mockInnerProvider;

    public CveProviderDecoratorTests()
    {
        _mockInnerProvider = Substitute.For<ICveDataProvider>();
    }

    [Fact]
    public async Task CacheDecorator_ReturnsCachedResult_WhenWithinTimeout()
    {
        // Arrange
        var cveId = "CVE-2023-1234";
        var cve = new Models.CVE { Id = cveId };
        _mockInnerProvider.GetCveByIdAsync(cveId).Returns(cve);

        var cacheDecorator = new CveProviderCacheDecorator(_mockInnerProvider, TimeSpan.FromMinutes(5));

        // Act
        var result1 = await cacheDecorator.GetCveByIdAsync(cveId);
        var result2 = await cacheDecorator.GetCveByIdAsync(cveId);

        // Assert
        Assert.Equal(cve.Id, result1?.Id);
        Assert.Equal(cve.Id, result2?.Id);
        await _mockInnerProvider.Received(1).GetCveByIdAsync(cveId);
    }

    [Fact]
    public async Task CacheDecorator_RefetchesData_WhenCacheExpired()
    {
        // Arrange
        var cveId = "CVE-2023-1234";
        var cve = new Models.CVE { Id = cveId };
        _mockInnerProvider.GetCveByIdAsync(cveId).Returns(cve);

        var cacheDecorator = new CveProviderCacheDecorator(_mockInnerProvider, TimeSpan.FromMilliseconds(1));

        // Act
        var result1 = await cacheDecorator.GetCveByIdAsync(cveId);
        await Task.Delay(10); // Wait for cache to expire
        var result2 = await cacheDecorator.GetCveByIdAsync(cveId);

        // Assert
        Assert.Equal(cve.Id, result1?.Id);
        Assert.Equal(cve.Id, result2?.Id);
        await _mockInnerProvider.Received(2).GetCveByIdAsync(cveId);
    }

    [Fact]
    public async Task LoggingDecorator_LogsOperations()
    {
        // Arrange
        var cveId = "CVE-2023-1234";
        var cve = new Models.CVE { Id = cveId };
        _mockInnerProvider.GetCveByIdAsync(cveId).Returns(cve);

        var loggerFactory = LoggerFactory.Create(builder => builder.AddInMemoryLogger());
        var logger = loggerFactory.CreateLogger<CveProviderLoggingDecorator>();
        var loggingDecorator = new CveProviderLoggingDecorator(_mockInnerProvider, logger);

        // Act
        await loggingDecorator.GetCveByIdAsync(cveId);

        // Assert
        var logs = loggerFactory.GetInMemoryLogMessages();
        Assert.Contains(logs, log => log.Contains($"Buscando CVE com ID: {cveId}"));
        Assert.Contains(logs, log => log.Contains($"CVE {cveId} encontrado: True"));
    }
}