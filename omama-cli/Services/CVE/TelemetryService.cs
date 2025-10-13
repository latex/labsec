using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;

namespace omama_cli.Services.CVE;

public class TelemetryService
{
    private static readonly bool _enabled = Environment.GetEnvironmentVariable("OMAMA_STAT") == "homol";
    private static readonly ConcurrentDictionary<string, TelemetryData> _metrics = new();
    private static readonly object _lockObject = new();

    public static void RecordMetric(string operation, string provider, TimeSpan duration, bool success, string? errorMessage = null, Dictionary<string, object>? additionalData = null)
    {
        if (!_enabled) return;

        var key = $"{provider}:{operation}";
        var data = _metrics.GetOrAdd(key, _ => new TelemetryData(provider, operation));
        
        lock (_lockObject)
        {
            data.TotalCalls++;
            data.TotalDuration += duration;
            
            if (success)
            {
                data.SuccessCount++;
            }
            else
            {
                data.ErrorCount++;
                if (!string.IsNullOrEmpty(errorMessage))
                {
                    data.RecentErrors.Add($"{DateTime.Now:HH:mm:ss} - {errorMessage}");
                    if (data.RecentErrors.Count > 10)
                    {
                        data.RecentErrors.RemoveAt(0);
                    }
                }
            }

            if (duration > data.MaxDuration)
                data.MaxDuration = duration;
            
            if (data.MinDuration == TimeSpan.Zero || duration < data.MinDuration)
                data.MinDuration = duration;

            if (additionalData != null)
            {
                foreach (var kvp in additionalData)
                {
                    data.AdditionalMetrics[kvp.Key] = kvp.Value;
                }
            }
        }

        LogMetric(provider, operation, duration, success, errorMessage);
    }

    public static void RecordHttpMetric(string provider, string url, int statusCode, TimeSpan duration, long? contentLength = null)
    {
        if (!_enabled) return;

        var additionalData = new Dictionary<string, object>
        {
            ["StatusCode"] = statusCode,
            ["Url"] = url
        };

        if (contentLength.HasValue)
            additionalData["ContentLength"] = contentLength.Value;

        RecordMetric("HTTP", provider, duration, statusCode >= 200 && statusCode < 400, 
                    statusCode >= 400 ? $"HTTP {statusCode}" : null, additionalData);
    }

    public static void RecordMemoryUsage()
    {
        if (!_enabled) return;

        var process = Process.GetCurrentProcess();
        var memoryMB = process.WorkingSet64 / (1024 * 1024);
        
        Console.WriteLine($"[TELEMETRY] Memory Usage: {memoryMB} MB");
    }

    public static void LogMetric(string provider, string operation, TimeSpan duration, bool success, string? errorMessage)
    {
        if (!_enabled) return;

        var status = success ? "✓" : "✗";
        var error = !string.IsNullOrEmpty(errorMessage) ? $" - {errorMessage}" : "";
        
        Console.WriteLine($"[TELEMETRY] {status} {provider}.{operation}: {duration.TotalMilliseconds:F0}ms{error}");
    }

    public static void PrintSummary()
    {
        if (!_enabled || _metrics.IsEmpty) return;

        Console.WriteLine("\n=== TELEMETRY SUMMARY ===");
        
        foreach (var kvp in _metrics.OrderBy(x => x.Key))
        {
            var data = kvp.Value;
            var avgDuration = data.TotalCalls > 0 ? data.TotalDuration.TotalMilliseconds / data.TotalCalls : 0;
            var successRate = data.TotalCalls > 0 ? (data.SuccessCount * 100.0 / data.TotalCalls) : 0;
            
            Console.WriteLine($"\n{data.Provider}.{data.Operation}:");
            Console.WriteLine($"  Calls: {data.TotalCalls} | Success: {data.SuccessCount} | Errors: {data.ErrorCount}");
            Console.WriteLine($"  Success Rate: {successRate:F1}%");
            Console.WriteLine($"  Duration: Avg={avgDuration:F0}ms, Min={data.MinDuration.TotalMilliseconds:F0}ms, Max={data.MaxDuration.TotalMilliseconds:F0}ms");
            
            if (data.RecentErrors.Count > 0)
            {
                Console.WriteLine($"  Recent Errors: {string.Join(", ", data.RecentErrors.TakeLast(3))}");
            }

            if (data.AdditionalMetrics.Count > 0)
            {
                var metrics = string.Join(", ", data.AdditionalMetrics.Select(x => $"{x.Key}={x.Value}"));
                Console.WriteLine($"  Additional: {metrics}");
            }
        }
        
        RecordMemoryUsage();
        Console.WriteLine("=== END TELEMETRY ===\n");
    }

    public static string GetTelemetryJson()
    {
        if (!_enabled) return "{}";

        var summary = _metrics.ToDictionary(
            kvp => kvp.Key,
            kvp => new
            {
                kvp.Value.Provider,
                kvp.Value.Operation,
                kvp.Value.TotalCalls,
                kvp.Value.SuccessCount,
                kvp.Value.ErrorCount,
                SuccessRate = kvp.Value.TotalCalls > 0 ? kvp.Value.SuccessCount * 100.0 / kvp.Value.TotalCalls : 0,
                AvgDurationMs = kvp.Value.TotalCalls > 0 ? kvp.Value.TotalDuration.TotalMilliseconds / kvp.Value.TotalCalls : 0,
                MinDurationMs = kvp.Value.MinDuration.TotalMilliseconds,
                MaxDurationMs = kvp.Value.MaxDuration.TotalMilliseconds,
                kvp.Value.RecentErrors,
                kvp.Value.AdditionalMetrics
            }
        );

        return JsonSerializer.Serialize(summary, new JsonSerializerOptions { WriteIndented = true });
    }
}

public class TelemetryData
{
    public string Provider { get; }
    public string Operation { get; }
    public int TotalCalls { get; set; }
    public int SuccessCount { get; set; }
    public int ErrorCount { get; set; }
    public TimeSpan TotalDuration { get; set; }
    public TimeSpan MinDuration { get; set; }
    public TimeSpan MaxDuration { get; set; }
    public List<string> RecentErrors { get; } = new();
    public Dictionary<string, object> AdditionalMetrics { get; } = new();

    public TelemetryData(string provider, string operation)
    {
        Provider = provider;
        Operation = operation;
    }
}