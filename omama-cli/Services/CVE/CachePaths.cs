namespace omama_cli.Services.CVE;

public static class CachePaths
{
    public static string ResolveCacheDir()
    {
        var env = Environment.GetEnvironmentVariable("OMAMA_CACHE_DIR");
        if (!string.IsNullOrWhiteSpace(env))
        {
            try { Directory.CreateDirectory(env!); } catch { /* ignore: caller will fallback if needed */ }
            return env!;
        }

        // Try system-wide path
    const string sysCache = "/var/lib/omama-cli/cache";
        try { Directory.CreateDirectory(sysCache); return sysCache; } catch { /* not permitted */ }

        // Fallback to user-local
        var local = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "omama-cli", "cache");
        try { Directory.CreateDirectory(local); } catch { /* ignore */ }
        return local;
    }

    public static string ResolveRunDir()
    {
        var env = Environment.GetEnvironmentVariable("OMAMA_RUN_DIR");
        if (!string.IsNullOrWhiteSpace(env))
        {
            try { Directory.CreateDirectory(env!); } catch { /* ignore */ }
            return env!;
        }

        // Prefer system run dir
    const string sysRun = "/var/run/omama-cli";
        try { Directory.CreateDirectory(sysRun); return sysRun; } catch { /* not permitted */ }

        // Fallbacks
        var state = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "omama-cli", "run");
        try { Directory.CreateDirectory(state); return state; } catch { /* ignore */ }

        var tmp = Path.Combine(Path.GetTempPath(), "omama-cli");
        try { Directory.CreateDirectory(tmp); } catch { /* ignore */ }
        return tmp;
    }
}
