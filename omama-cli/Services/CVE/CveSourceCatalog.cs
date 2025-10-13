namespace omama_cli.Services.CVE;

public static class CveSourceCatalog
{
    public sealed record Source(string Name, string Url, bool BuiltIn = false);

    // Catalog of known sources (excluding the ones already existing in options listing duplicates)
    public static readonly IReadOnlyList<Source> All = new List<Source>
    {
        new("CVE.ORG", "https://www.cve.org/"),
        new("CVEDETAILS", "https://www.cvedetails.com/"),
    // Use the NVD REST API endpoint to ensure JSON responses
    new("NVD", "https://services.nvd.nist.gov/rest/json/cves/2.0", BuiltIn: true),
        new("VULNERS", "https://vulners.com/"),
        new("VULDB", "https://vuldb.com/"),
        new("WIZ", "https://www.wiz.io/pt-br/vulnerability-database"),
        new("EXPLOIT-DB", "https://www.exploit-db.com/"),
        new("IVANTI", "https://help.ivanti.com/ld/help/pt_BR/LDMS/10.0/Windows/patch-t-search-cve-name.htm"),
        new("CIRCL", "https://api.circl.lu/v1/cve", BuiltIn: true),
    };
}
