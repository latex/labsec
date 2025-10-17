using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using omama_cli.Models;
using omama_cli.Services.CVE;
using Xunit;

namespace Omama.Cli.tests;

public class CveQueryServiceTests
{
    private static string TempDir()
    {
        var dir = Path.Combine(Path.GetTempPath(), "omama-tests", "query", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        return dir;
    }

    private static async Task SeedAsync(string dir, params CVE[] items)
    {
        var cache = new HighPerformanceCache(dir);
        foreach (var c in items)
        {
            await cache.SetAsync($"cve:id:{c.Id}", c);
        }
    }

    private static CVE Make(string id, string sev, double score, DateTime published, string desc)
        => new CVE
        {
            Id = id,
            Severity = sev,
            Score = score,
            PublishedDate = published,
            LastModifiedDate = published.AddDays(1),
            Description = desc,
            References = new()
        };

    [Fact]
    public async Task Paginates_And_Reports_Total()
    {
        var d = TempDir();
        var a = Make("CVE-1", "LOW", 3.0, new DateTime(2025,1,1), "alpha");
        var b = Make("CVE-2", "HIGH", 9.1, new DateTime(2025,2,1), "bravo");
        var c = Make("CVE-3", "MEDIUM", 5.0, new DateTime(2025,3,1), "charlie");
        await SeedAsync(d, a,b,c);

        var svc = new CveQueryService(d);
        var res = await svc.ListSavedAsync(new CveQueryOptions(Page:1, PageSize:2));

        Assert.Equal(3, res.Total);
        Assert.Equal(2, res.Items.Count);
        Assert.Equal(1, res.Page);
        Assert.Equal(2, res.PageSize);
    }

    [Fact]
    public async Task Filters_By_Text_Severity_And_Score()
    {
        var d = TempDir();
        var a = Make("CVE-10", "LOW", 3.0, new DateTime(2025,1,1), "openssl alpha");
        var b = Make("CVE-20", "HIGH", 9.1, new DateTime(2025,2,1), "bravo libssl");
        var c = Make("CVE-30", "MEDIUM", 5.0, new DateTime(2025,3,1), "random text");
        await SeedAsync(d, a,b,c);

        var svc = new CveQueryService(d);
        var opts = new CveQueryOptions(
            Q: "ssl",
            Severities: new []{ "HIGH" },
            MinScore: 8.0,
            Page: 1,
            PageSize: 10);
        var res = await svc.ListSavedAsync(opts);

        Assert.Equal(1, res.Total);
        Assert.Single(res.Items);
        Assert.Equal("CVE-20", res.Items[0].Id);
    }

    [Fact]
    public async Task Sorts_By_Score_Ascending()
    {
        var d = TempDir();
        var a = Make("CVE-A", "LOW", 2.0, new DateTime(2025,1,1), "a");
        var b = Make("CVE-B", "HIGH", 9.0, new DateTime(2025,1,2), "b");
        var c = Make("CVE-C", "MEDIUM", 5.0, new DateTime(2025,1,3), "c");
        await SeedAsync(d, a,b,c);

        var svc = new CveQueryService(d);
        var opts = new CveQueryOptions(SortBy: "score", Desc: false, Page:1, PageSize:10);
        var res = await svc.ListSavedAsync(opts);

        Assert.Equal(new[]{"CVE-A","CVE-C","CVE-B"}, res.Items.Select(x=>x.Id).ToArray());
    }

    [Fact]
    public async Task Filters_By_Date_Range()
    {
        var d = TempDir();
        var a = Make("CVE-01", "LOW", 1.0, new DateTime(2025,1,10), "a");
        var b = Make("CVE-02", "LOW", 1.0, new DateTime(2025,2,10), "b");
        var c = Make("CVE-03", "LOW", 1.0, new DateTime(2025,3,10), "c");
        await SeedAsync(d, a,b,c);

        var svc = new CveQueryService(d);
        var opts = new CveQueryOptions(
            Since: new DateTimeOffset(new DateTime(2025,2,1), TimeSpan.Zero),
            Until: new DateTimeOffset(new DateTime(2025,3,1), TimeSpan.Zero),
            Page:1, PageSize:10);
        var res = await svc.ListSavedAsync(opts);

        Assert.Equal(1, res.Total);
        Assert.Equal("CVE-02", res.Items.Single().Id);
    }
}
