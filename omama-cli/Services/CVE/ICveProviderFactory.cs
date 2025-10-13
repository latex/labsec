namespace omama_cli.Services.CVE;

public interface ICveProviderFactory
{
    (ICveDataProvider Provider, ParallelCveProcessor Processor) Create();
}
