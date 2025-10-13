namespace omama_cli.Services.CVE;

public interface ICveMergeStrategy
{
    Models.CVE MergeCves(IEnumerable<Models.CVE> cves);
    IEnumerable<Models.CVE> MergeCvesList(IEnumerable<Models.CVE> cves);
}

public class DefaultCveMergeStrategy : ICveMergeStrategy
{
    public Models.CVE MergeCves(IEnumerable<Models.CVE> cves)
    {
        var cveList = cves.ToList();
        if (!cveList.Any())
            throw new ArgumentException("No CVEs to merge");

        var firstCve = cveList.First();
        if (cveList.Count == 1)
            return firstCve;

        var descriptions = new HashSet<string>();
        var references = new HashSet<string>();
        var scores = new List<double>();

        foreach (var cve in cveList)
        {
            if (!string.IsNullOrWhiteSpace(cve.Description))
                descriptions.Add(cve.Description);
            
            foreach (var reference in cve.References)
                references.Add(reference);
            
            if (cve.Score > 0)
                scores.Add(cve.Score);
        }

        return new Models.CVE
        {
            Id = firstCve.Id,
            Description = string.Join("\n\n", descriptions),
            PublishedDate = cveList.Min(c => c.PublishedDate),
            LastModifiedDate = cveList.Max(c => c.LastModifiedDate),
            Score = scores.Any() ? scores.Max() : 0.0,
            Severity = DetermineSeverity(cveList.Select(c => c.Severity)),
            References = references.ToList()
        };
    }

    public IEnumerable<Models.CVE> MergeCvesList(IEnumerable<Models.CVE> cves)
    {
        return cves.GroupBy(c => c.Id)
                  .Select(g => MergeCves(g));
    }

    private string DetermineSeverity(IEnumerable<string> severities)
    {
        var severityOrder = new Dictionary<string, int>
        {
            { "CRITICAL", 4 },
            { "HIGH", 3 },
            { "MEDIUM", 2 },
            { "LOW", 1 },
            { "NONE", 0 }
        };

        return severities.Where(s => !string.IsNullOrWhiteSpace(s))
                        .OrderByDescending(s => severityOrder.GetValueOrDefault(s.ToUpper(), -1))
                        .FirstOrDefault() ?? "NONE";
    }
}