namespace omama_cli.Services.CVE;

public static class CveSourceCatalog
{
    public sealed record Source(string Name, string Url, bool BuiltIn = false);

    // Static URLs for convenience
    public const string NVD_URL = "https://services.nvd.nist.gov/rest/json/cves/2.0";
    public const string CIRCL_URL = "https://cve.circl.lu/api";
    public const string CVEORG_URL = "https://www.cve.org/";
    public const string CVEDETAILS_URL = "https://www.cvedetails.com/";
    public const string VULNERS_URL = "https://vulners.com/";

    // Catalog of known sources (excluding the ones already existing in options listing duplicates)
    public static readonly IReadOnlyList<Source> All = new List<Source>
    {
        new("CVE.ORG", CVEORG_URL),
        new("CVEDETAILS", CVEDETAILS_URL),
    // Use the NVD REST API endpoint to ensure JSON responses
    new("NVD", NVD_URL, BuiltIn: true),
        new("VULNERS", VULNERS_URL),
        new("VULDB", "https://vuldb.com/"),
        new("WIZ", "https://www.wiz.io/pt-br/vulnerability-database"),
        new("EXPLOIT-DB", "https://www.exploit-db.com/"),
        new("IVANTI", "https://help.ivanti.com/ld/help/pt_BR/LDMS/10.0/Windows/patch-t-search-cve-name.htm"),
        new("CIRCL", CIRCL_URL, BuiltIn: true),
    };
}
