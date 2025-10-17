using System;
using System.IO;
using System.Threading.Tasks;
using NSubstitute;
using omama_cli.Models;
using omama_cli.Services.CVE;
using Xunit;

namespace Omama.Cli.tests;

public class CveSyncServiceForceTests
{
    private static ICveDataProvider CreateProvider(params CVE[] items)
    {
        var provider = Substitute.For<ICveDataProvider>();
        provider.SearchCvesAsync(Arg.Any<string>()).Returns(items);
        provider.GetLatestCvesAsync(Arg.Any<int>()).Returns(items);
        foreach (var cve in items)
        {
            provider.GetCveByIdAsync(cve.Id).Returns(cve);
        }
        return provider;
    }

    [Fact]
    public async Task Sync_ForceTrue_FetchesEvenIfExists()
    {
        var cve = new CVE { Id = "CVE-2025-0100" };
        var provider = CreateProvider(cve);
        var cacheDir = Path.Combine(Path.GetTempPath(), "omama-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(cacheDir);
        var service = new CveSyncService(provider, cacheDir, maxParallelism: 2);

        // Pre-existente
        var prePath = Path.Combine(cacheDir, $"cve:id:{cve.Id}.json.gz");
        await File.WriteAllTextAsync(prePath, "dummy");

        var result = await service.SyncAsync(keyword: null, latest: 1, force: true);

        Assert.Equal(1, result.Scanned);
        Assert.Equal(1, result.Existed);
        Assert.Equal(1, result.Saved); // reprocessado
        await provider.Received(1).GetCveByIdAsync(cve.Id);
    }
}
