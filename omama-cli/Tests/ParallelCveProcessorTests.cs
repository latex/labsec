namespace omama_cli.Tests;

public class ParallelCveProcessorTests
{
    [Fact]
    public async Task ProcessCves_WithMultipleProviders_ProcessesInParallel()
    {
        // Arrange
        var mockProvider1 = Substitute.For<ICveDataProvider>();
        var mockProvider2 = Substitute.For<ICveDataProvider>();
        var cves = Enumerable.Range(1, 10).Select(i => new Models.CVE 
        { 
            Id = $"CVE-2023-{i:D4}",
            Description = $"Test vulnerability {i}"
        });

        mockProvider1.SearchCvesAsync(Arg.Any<string>())
            .Returns(cves);
        mockProvider2.SearchCvesAsync(Arg.Any<string>())
            .Returns(cves);

        var processor = new ParallelCveProcessor(4); // 4 threads
        var composite = new CompositeCveProvider(new[] { mockProvider1, mockProvider2 });

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var results = await processor.ProcessCvesAsync("test", composite);
        stopwatch.Stop();

        // Assert
        Assert.NotEmpty(results);
        await mockProvider1.Received(1).SearchCvesAsync(Arg.Any<string>());
        await mockProvider2.Received(1).SearchCvesAsync(Arg.Any<string>());
    }

    [Fact]
    public async Task ProcessCves_WithCancellation_StopsProcessing()
    {
        // Arrange
        var mockProvider = Substitute.For<ICveDataProvider>();
        using (var cts = new CancellationTokenSource())
        {
            var processor = new ParallelCveProcessor(4);
            
            mockProvider.SearchCvesAsync(Arg.Any<string>())
                .Returns(async _ => 
                {
                    await Task.Delay(1000); // Simula processamento longo
                    return new[] { new Models.CVE { Id = "CVE-2023-0001" } };
                });

            // Act & Assert
            cts.CancelAfter(100); // Cancela após 100ms
            await Assert.ThrowsAsync<OperationCanceledException>(() =>
                processor.ProcessCvesAsync("test", mockProvider, cts.Token));
        }
    }
}