namespace omama_cli.Models;

public class CVE
{
    public string Id { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public double Score { get; set; }
    public string Severity { get; set; } = string.Empty;
    public DateTime PublishedDate { get; set; }
    public DateTime LastModifiedDate { get; set; }
    public List<string> References { get; set; } = new();
}