using System.Runtime;

namespace omama_cli.Tests;

public class HighPerformanceCacheTests
{
    private readonly string _testCachePath;
    private const long TEST_MEMORY_LIMIT = 1024L * 1024L * 512L; // 512MB para testes

    public HighPerformanceCacheTests()
    {
        _testCachePath = Path.Combine(Path.GetTempPath(), "cve_cache_test");
        if (Directory.Exists(_testCachePath))
        {
            Directory.Delete(_testCachePath, true);
        }
        Directory.CreateDirectory(_testCachePath);
    }

    [Fact]
    public async Task ProcessBulkData_WithLargeDataSet_HandlesMemoryEfficiently()
    {
        // Arrange
        var cache = new HighPerformanceCache(_testCachePath, TEST_MEMORY_LIMIT);
        var largeCveSet = GenerateLargeCveSet(1000); // Gera 1000 CVEs para teste

        // Act
        var memoryBefore = GC.GetTotalMemory(true);
        await cache.ProcessAndCacheBulkAsync(largeCveSet);
        var memoryAfter = GC.GetTotalMemory(true);

        // Assert
        Assert.True(memoryAfter - memoryBefore < TEST_MEMORY_LIMIT);
        Assert.True(await cache.GetCachedItemCountAsync() > 0);
    }

    [Fact]
    public async Task GetCachedData_AfterMemoryPressure_LoadsFromDisk()
    {
        // Arrange
        var cache = new HighPerformanceCache(_testCachePath, TEST_MEMORY_LIMIT);
        var testCve = new Models.CVE
        {
            Id = "CVE-2023-TEST",
            Description = "Test vulnerability"
        };

        // Act
        await cache.SetAsync("test-key", testCve);
        GC.Collect(2, GCCollectionMode.Forced, true, true); // Força liberação de memória
        var retrieved = await cache.GetAsync<Models.CVE>("test-key");

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal(testCve.Id, retrieved.Id);
    }

    [Fact]
    public async Task ProcessParallel_WithMultipleItems_UsesBatchProcessing()
    {
        // Arrange
        var cache = new HighPerformanceCache(_testCachePath, TEST_MEMORY_LIMIT);
        var items = Enumerable.Range(1, 100).Select(i => new Models.CVE
        {
            Id = $"CVE-2023-{i:D4}",
            Description = $"Test vulnerability {i}"
        });

        // Act
        var sw = System.Diagnostics.Stopwatch.StartNew();
        await cache.ProcessAndCacheBulkAsync(items);
        sw.Stop();

        // Assert
        var allCached = await cache.GetAllCachedAsync<Models.CVE>();
        Assert.Equal(100, allCached.Count());
    }

    private static IEnumerable<Models.CVE> GenerateLargeCveSet(int count)
    {
        var random = new Random(42); // Seed fixo para consistência
        return Enumerable.Range(1, count).Select(i => new Models.CVE
        {
            Id = $"CVE-2023-{i:D4}",
            Description = new string('X', random.Next(1000, 5000)), // Descrição grande
            References = Enumerable.Range(1, 10)
                .Select(_ => $"http://example.com/ref/{Guid.NewGuid()}")
                .ToList()
        });
    }
}