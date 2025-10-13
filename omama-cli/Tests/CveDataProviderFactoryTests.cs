using Microsoft.Extensions.Options;

namespace omama_cli.Tests;

public class CveDataProviderFactoryTests
{
    [Fact]
    public void Create_WithNvdProvider_ReturnsNvdProviderInstance()
    {
        // Arrange
        var options = Options.Create(new CveProviderOptions
        {
            Provider = "NVD",
            BaseUrl = "https://services.nvd.nist.gov/rest/json/cves/2.0"
        });
        var factory = new CveDataProviderFactory(options);

        // Act
        var provider = factory.Create();

        // Assert
        Assert.IsType<NvdCveProvider>(provider);
    }

    [Fact]
    public void Create_WithInvalidProvider_ThrowsArgumentException()
    {
        // Arrange
        var options = Options.Create(new CveProviderOptions
        {
            Provider = "InvalidProvider",
            BaseUrl = "https://example.com"
        });
        var factory = new CveDataProviderFactory(options);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => factory.Create());
    }
}