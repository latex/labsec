namespace omama_cli.Services.CVE;

public record NamedCveProvider(string Name, ICveDataProvider Provider);
