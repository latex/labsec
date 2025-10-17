using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using NSubstitute;
using omama_cli.Models;
using omama_cli.Services.CVE;
using Xunit;

namespace Omama.Cli.tests;

public class CveSyncServiceTests
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

    private static string CreateTempDir()
    {
        var dir = Path.Combine(Path.GetTempPath(), "omama-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        return dir;
    }

    [Fact]
    public async Task Sync_SkipsExisting_WhenForceFalse()
    {
        var cve = new CVE { Id = "CVE-2025-0001" };
        var provider = CreateProvider(cve);
        var cacheDir = CreateTempDir();
        var service = new CveSyncService(provider, cacheDir, maxParallelism: 2);

        // Cria arquivo de cache pré-existente
        var prePath = Path.Combine(cacheDir, $"cve:id:{cve.Id}.json.gz");
        await File.WriteAllTextAsync(prePath, "dummy");

        var result = await service.SyncAsync(keyword: null, latest: 1);

        Assert.Equal(1, result.Scanned);
        Assert.Equal(1, result.Existed);
        Assert.Equal(0, result.Saved);
        await provider.DidNotReceive().GetCveByIdAsync(cve.Id);
    }

    [Fact]
    public async Task Sync_SavesMissing_WhenForceFalse()
    {
        var cve = new CVE { Id = "CVE-2025-0002" };
        var provider = CreateProvider(cve);
        var cacheDir = CreateTempDir();
        var service = new CveSyncService(provider, cacheDir, maxParallelism: 2);

        var result = await service.SyncAsync(keyword: null, latest: 1);

        Assert.Equal(1, result.Scanned);
        Assert.Equal(0, result.Existed);
        Assert.Equal(1, result.Saved);
        await provider.Received(1).GetCveByIdAsync(cve.Id);
    }
}
