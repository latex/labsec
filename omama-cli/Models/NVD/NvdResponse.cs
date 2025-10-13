using System.Text.Json.Serialization;

namespace omama_cli.Models.NVD;

public class NvdResponse
{
    [JsonPropertyName("vulnerabilities")]
    public List<NvdVulnerability> Vulnerabilities { get; set; } = new();
}

public class NvdVulnerability
{
    [JsonPropertyName("cve")]
    public NvdCveData Cve { get; set; } = new();
}

public class NvdCveData
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("descriptions")]
    public List<NvdDescription> Descriptions { get; set; } = new();

    [JsonPropertyName("published")]
    public DateTime Published { get; set; }

    [JsonPropertyName("lastModified")]
    public DateTime LastModified { get; set; }

    [JsonPropertyName("metrics")]
    public NvdMetrics? Metrics { get; set; }

    [JsonPropertyName("references")]
    public List<NvdReference> References { get; set; } = new();
}

public class NvdDescription
{
    [JsonPropertyName("value")]
    public string Value { get; set; } = string.Empty;
}

public class NvdMetrics
{
    [JsonPropertyName("cvssMetricV31")]
    public List<NvdCvssMetric> CvssMetricV31 { get; set; } = new();
}

public class NvdCvssMetric
{
    [JsonPropertyName("cvssData")]
    public NvdCvssData CvssData { get; set; } = new();
}

public class NvdCvssData
{
    [JsonPropertyName("baseScore")]
    public double BaseScore { get; set; }

    [JsonPropertyName("baseSeverity")]
    public string BaseSeverity { get; set; } = string.Empty;
}

public class NvdReference
{
    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;
}