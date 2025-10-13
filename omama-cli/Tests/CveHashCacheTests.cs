namespace omama_cli.Tests;

public class CveHashCacheTests
{
    private readonly string _testCachePath;

    public CveHashCacheTests()
    {
        _testCachePath = Path.Combine(Path.GetTempPath(), "cve_cache_test");
        if (Directory.Exists(_testCachePath))
        {
            Directory.Delete(_testCachePath, true);
        }
        Directory.CreateDirectory(_testCachePath);
    }

    [Fact]
    public async Task GetOrCreateAsync_NewData_CreatesCache()
    {
        // Arrange
        var cache = new CveHashCache(_testCachePath);
        var cve = new Models.CVE
        {
            Id = "CVE-2023-1234",
            Description = "Test vulnerability"
        };
        var key = "test-key";

        // Act
        var result = await cache.GetOrCreateAsync(key, async () => cve);

        // Assert
        Assert.Equal(cve.Id, result.Id);
        Assert.True(File.Exists(Path.Combine(_testCachePath, $"{ComputeHash(key)}.json")));
    }

    [Fact]
    public async Task GetOrCreateAsync_ExpiredData_UpdatesCache()
    {
        // Arrange
        var cache = new CveHashCache(_testCachePath);
        var oldCve = new Models.CVE
        {
            Id = "CVE-2023-1234",
            Description = "Old data",
            LastModifiedDate = DateTime.UtcNow.AddDays(-6) // Dados velhos
        };
        var newCve = new Models.CVE
        {
            Id = "CVE-2023-1234",
            Description = "New data",
            LastModifiedDate = DateTime.UtcNow
        };
        var key = "test-key";

        // Primeiro, armazena dados antigos
        await cache.GetOrCreateAsync(key, async () => oldCve);

        // Act
        var result = await cache.GetOrCreateAsync(key, async () => newCve);

        // Assert
        Assert.Equal("New data", result.Description);
    }

    [Fact]
    public async Task GetOrCreateAsync_ValidCache_ReturnsCachedData()
    {
        // Arrange
        var cache = new CveHashCache(_testCachePath);
        var cve = new Models.CVE
        {
            Id = "CVE-2023-1234",
            Description = "Cached data",
            LastModifiedDate = DateTime.UtcNow
        };
        var key = "test-key";

        // Primeiro, armazena os dados
        await cache.GetOrCreateAsync(key, async () => cve);

        var fetchCount = 0;
        // Act
        var result = await cache.GetOrCreateAsync(key, async () =>
        {
            fetchCount++;
            return new Models.CVE { Id = "CVE-2023-5678", Description = "New data" };
        });

        // Assert
        Assert.Equal(cve.Id, result.Id);
        Assert.Equal(0, fetchCount); // Não deve ter buscado novos dados
    }

    private static string ComputeHash(string input)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var bytes = System.Text.Encoding.UTF8.GetBytes(input);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToHexString(hash);
    }
}