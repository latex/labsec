using omama_cli.Services.CVE;
using RichardSzalay.MockHttp;

namespace omama_cli.Tests;

public class NvdCveProviderTests
{
    private readonly MockHttpMessageHandler _mockHttp;
    private readonly ICveDataProvider _cveProvider;
    private readonly string _baseUrl = "https://services.nvd.nist.gov/rest/json/cves/2.0";

    public NvdCveProviderTests()
    {
        _mockHttp = new MockHttpMessageHandler();
        var client = _mockHttp.ToHttpClient();
        _cveProvider = new NvdCveProvider(client);
    }

    [Fact]
    public async Task GetCveByIdAsync_ValidId_ReturnsCve()
    {
        // Arrange
        var cveId = "CVE-2023-1234";
        var mockResponse = @"{
            ""vulnerabilities"": [
                {
                    ""cve"": {
                        ""id"": ""CVE-2023-1234"",
                        ""descriptions"": [
                            {
                                ""value"": ""Test vulnerability description""
                            }
                        ],
                        ""published"": ""2023-01-01T00:00:00.000"",
                        ""lastModified"": ""2023-01-02T00:00:00.000"",
                        ""metrics"": {
                            ""cvssMetricV31"": [
                                {
                                    ""cvssData"": {
                                        ""baseScore"": 7.5,
                                        ""baseSeverity"": ""HIGH""
                                    }
                                }
                            ]
                        },
                        ""references"": [
                            {
                                ""url"": ""https://example.com/cve-ref""
                            }
                        ]
                    }
                }
            ]
        }";

        _mockHttp.When($"{_baseUrl}?cveId={cveId}")
                .Respond("application/json", mockResponse);

        // Act
        var result = await _cveProvider.GetCveByIdAsync(cveId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(cveId, result.Id);
        Assert.Equal("Test vulnerability description", result.Description);
        Assert.Equal(7.5, result.Score);
        Assert.Equal("HIGH", result.Severity);
        Assert.Single(result.References);
    }

    [Fact]
    public async Task GetCveByIdAsync_InvalidId_ReturnsNull()
    {
        // Arrange
        var cveId = "invalid-id";
        _mockHttp.When($"{_baseUrl}?cveId={cveId}")
                .Respond(System.Net.HttpStatusCode.NotFound);

        // Act
        var result = await _cveProvider.GetCveByIdAsync(cveId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task SearchCvesAsync_ValidKeyword_ReturnsCves()
    {
        // Arrange
        var keyword = "test";
        var mockResponse = @"{
            ""vulnerabilities"": [
                {
                    ""cve"": {
                        ""id"": ""CVE-2023-1234"",
                        ""descriptions"": [
                            {
                                ""value"": ""Test vulnerability description""
                            }
                        ],
                        ""published"": ""2023-01-01T00:00:00.000"",
                        ""lastModified"": ""2023-01-02T00:00:00.000"",
                        ""metrics"": {
                            ""cvssMetricV31"": [
                                {
                                    ""cvssData"": {
                                        ""baseScore"": 7.5,
                                        ""baseSeverity"": ""HIGH""
                                    }
                                }
                            ]
                        },
                        ""references"": [
                            {
                                ""url"": ""https://example.com/cve-ref""
                            }
                        ]
                    }
                }
            ]
        }";

        _mockHttp.When($"{_baseUrl}?keywordSearch={keyword}")
                .Respond("application/json", mockResponse);

        // Act
        var results = await _cveProvider.SearchCvesAsync(keyword);

        // Assert
        var resultList = results.ToList();
        Assert.Single(resultList);
        Assert.Equal("CVE-2023-1234", resultList[0].Id);
        Assert.Equal("Test vulnerability description", resultList[0].Description);
        Assert.Equal(7.5, resultList[0].Score);
        Assert.Equal("HIGH", resultList[0].Severity);
        Assert.Single(resultList[0].References);
    }

    [Fact]
    public async Task GetLatestCvesAsync_ReturnsSpecifiedNumberOfCves()
    {
        // Arrange
        var limit = 2;
        var mockResponse = @"{
            ""vulnerabilities"": [
                {
                    ""cve"": {
                        ""id"": ""CVE-2023-0001"",
                        ""descriptions"": [
                            {
                                ""value"": ""First vulnerability""
                            }
                        ],
                        ""published"": ""2023-01-01T00:00:00.000"",
                        ""lastModified"": ""2023-01-02T00:00:00.000"",
                        ""metrics"": {
                            ""cvssMetricV31"": [
                                {
                                    ""cvssData"": {
                                        ""baseScore"": 7.5,
                                        ""baseSeverity"": ""HIGH""
                                    }
                                }
                            ]
                        },
                        ""references"": []
                    }
                },
                {
                    ""cve"": {
                        ""id"": ""CVE-2023-0002"",
                        ""descriptions"": [
                            {
                                ""value"": ""Second vulnerability""
                            }
                        ],
                        ""published"": ""2023-01-01T00:00:00.000"",
                        ""lastModified"": ""2023-01-02T00:00:00.000"",
                        ""metrics"": {
                            ""cvssMetricV31"": [
                                {
                                    ""cvssData"": {
                                        ""baseScore"": 5.5,
                                        ""baseSeverity"": ""MEDIUM""
                                    }
                                }
                            ]
                        },
                        ""references"": []
                    }
                }
            ]
        }";

        _mockHttp.When($"{_baseUrl}?resultsPerPage={limit}")
                .Respond("application/json", mockResponse);

        // Act
        var results = await _cveProvider.GetLatestCvesAsync(limit);

        // Assert
        var resultList = results.ToList();
        Assert.Equal(2, resultList.Count);
        Assert.Equal("CVE-2023-0001", resultList[0].Id);
        Assert.Equal("CVE-2023-0002", resultList[1].Id);
        Assert.Equal("First vulnerability", resultList[0].Description);
        Assert.Equal("Second vulnerability", resultList[1].Description);
        Assert.Equal(7.5, resultList[0].Score);
        Assert.Equal(5.5, resultList[1].Score);
    }

    [Fact]
    public async Task SearchCvesAsync_InvalidKeyword_ReturnsEmptyList()
    {
        // Arrange
        var keyword = "nonexistentcve123456";
        _mockHttp.When($"{_baseUrl}?keywordSearch={keyword}")
                .Respond("application/json", @"{""vulnerabilities"": []}");

        // Act
        var results = await _cveProvider.SearchCvesAsync(keyword);

        // Assert
        Assert.Empty(results);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetLatestCvesAsync_InvalidLimit_ReturnsEmptyList(int invalidLimit)
    {
        // Act
        var results = await _cveProvider.GetLatestCvesAsync(invalidLimit);

        // Assert
        Assert.Empty(results);
    }

    [Fact]
    public async Task SearchCvesAsync_ApiError_ReturnsEmptyList()
    {
        // Arrange
        var keyword = "test";
        _mockHttp.When($"{_baseUrl}?keywordSearch={keyword}")
                .Respond(System.Net.HttpStatusCode.InternalServerError);

        // Act
        var results = await _cveProvider.SearchCvesAsync(keyword);

        // Assert
        Assert.Empty(results);
    }
}