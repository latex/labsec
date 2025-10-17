using System.Text.Json;
using System.Globalization;
using omama_cli.Services.CVE;
using omama_cli.Services;
using Microsoft.Extensions.Options;

// Variável global de ambiente: "homol" ou "prod"
var stat = Environment.GetEnvironmentVariable("OMAMA_STAT") ?? "prod";

void Log(string msg)
{
    if (stat == "homol")
        Console.WriteLine($"[LOG] {msg}");
}

static void PrintHelp()
{
    Console.WriteLine("Omama CLI - Gerenciador de Diretivas");
    Console.WriteLine();
    Console.WriteLine("Uso:");
    Console.WriteLine("  omama-cli add --name <nome> --description <desc> --value <valor>");
    Console.WriteLine("  omama-cli list");
    Console.WriteLine("  omama-cli list source [--json]");
    Console.WriteLine("  omama-cli list cves [--q <text>] [--since <yyyy-MM-dd>] [--until <yyyy-MM-dd>] [--severity <comma>] [--minScore <n>] [--maxScore <n>] [--sort <published|modified|score|id>] [--asc] [--page <n>] [--pageSize <n>] [--format <text|md|json>]");
    Console.WriteLine("  omama-cli update --name <nome> --value <novoValor>");
    Console.WriteLine("  omama-cli delete --name <nome>");
    Console.WriteLine("  omama-cli sync [--keyword <kw> | --latest <n>] [--force] [--json]");
    Console.WriteLine("  omama-cli sync slow [--batch <n>] [--force] [--maxPerHour <n>] [--concurrency <n>] [--json]");
    Console.WriteLine("  omama-cli stats source [--json]");
    Console.WriteLine("  omama-cli count");
  Console.WriteLine("  omama-cli test provider <nome> [--batch N] [--curl] [--method GET|POST]");
}

static Dictionary<string, string> ParseOptions(string[] args, int startIndex)
{
    var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    int i = startIndex;
    while (i < args.Length)
    {
        var token = args[i];
        if (!token.StartsWith("--"))
            throw new ArgumentException($"Opção inválida: {token}");

        var key = token.Substring(2);
        // Flag sem valor (ex: --json)
        if (i + 1 >= args.Length || args[i + 1].StartsWith("--"))
        {
            dict[key] = "true";
            i = i + 1;
            continue;
        }

        // Par chave-valor
        dict[key] = args[i + 1];
        i = i + 2;
    }
    return dict;
}

var directiveService = new DirectiveService();

static async Task HandleCountCommand(Dictionary<string, string> opts)
{
    var asJson = opts.ContainsKey("json");
    var cacheDir = CachePaths.ResolveCacheDir();
    var query = new CveQueryService(cacheDir);
    var counts = await query.CountSavedBySourceAsync();

    if (asJson)
    {
        Console.WriteLine(JsonSerializer.Serialize(counts, new JsonSerializerOptions { WriteIndented = true }));
    }
    else
    {
        Console.WriteLine("Total de CVEs salvos por fonte (cache):");
        foreach (var kv in counts.OrderByDescending(k => k.Value))
        {
            Console.WriteLine($"- {kv.Key}: {kv.Value}");
        }
    }
}

async Task HandleTestCommand(string[] args)
{
    // Testa um provider específico: test provider <nome> [--batch N] [--curl] [--method GET|POST]
    if (args.Length < 3)
    {
        Console.WriteLine("Uso: omama-cli test provider <nome> [--batch N] [--curl] [--method GET|POST]");
        return;
    }

    var providerName = args[2].ToUpperInvariant();
    var opts = ParseOptions(args, 3);
    var batch = opts.TryGetValue("batch", out var batchStr) && int.TryParse(batchStr, out var batchInt) ? batchInt : 5;
    var useCurl = opts.ContainsKey("curl");
    var method = opts.GetValueOrDefault("method", "GET").ToUpperInvariant();

    await TestProviderAsync(providerName, batch, useCurl, method);
}

void HandleAddCommand(string[] args)
{
    var opts = ParseOptions(args, 1);
    var name = opts.GetValueOrDefault("name") ?? throw new ArgumentException("--name é obrigatório");
    var description = opts.GetValueOrDefault("description") ?? throw new ArgumentException("--description é obrigatório");
    var value = opts.GetValueOrDefault("value") ?? throw new ArgumentException("--value é obrigatório");
    directiveService.AddDirective(name, description, value);
    Console.WriteLine($"Diretiva '{name}' adicionada com sucesso!");
}

static DateTimeOffset? ParseDate(string? s)
{
    if (string.IsNullOrWhiteSpace(s)) return null;
    // Prefer ISO yyyy-MM-dd
    if (DateTimeOffset.TryParseExact(s, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var dto))
        return dto;
    if (DateTimeOffset.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out dto))
        return dto;
    return null;
}

if (args.Length == 0 || args[0] == "-h" || args[0] == "--help")
{
    PrintHelp();
    return;
}

var command = args[0].ToLowerInvariant();
try
{
    // Write PID file to indicate the app is running
    try
    {
        var runDir = CachePaths.ResolveRunDir();
        var pidFile = Path.Combine(runDir, "omama-cli.pid");
    await File.WriteAllTextAsync(pidFile, $"{Environment.ProcessId}\n{DateTimeOffset.UtcNow:o}\n");
    }
    catch { /* ignore errors writing pid file */ }

    switch (command)
    {
        case "count":
            await HandleCountCommand(ParseOptions(args, 1));
            break;
        case "test":
            await HandleTestCommand(args);
            break;
        case "add":
        {
            var opts = ParseOptions(args, 1);
            var name = opts.GetValueOrDefault("name") ?? throw new ArgumentException("--name é obrigatório");
            var description = opts.GetValueOrDefault("description") ?? throw new ArgumentException("--description é obrigatório");
            var value = opts.GetValueOrDefault("value") ?? throw new ArgumentException("--value é obrigatório");
            directiveService.AddDirective(name, description, value);
            Console.WriteLine($"Diretiva '{name}' adicionada com sucesso!");
            break;
        }
        case "list":
        {
            // Subcomando: list source
            if (args.Length > 1 && string.Equals(args[1], "source", StringComparison.OrdinalIgnoreCase))
            {
                bool asJson = args.Skip(2).Any(a => string.Equals(a, "--json", StringComparison.OrdinalIgnoreCase));
                var cveOptions = new CveProviderOptions();

                // Constrói a lista mesclando catálogo e opções atuais, sem duplicados
                var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var list = new List<SourceView>();

                // Built-ins a partir das opções
                void Add(string name, bool enabled, string url)
                {
                    if (seen.Add(name)) list.Add(new SourceView(name, enabled, url));
                }
                Add("NVD", cveOptions.EnableNvd, cveOptions.NvdBaseUrl);
                Add("CIRCL", cveOptions.EnableCircl, cveOptions.CirclBaseUrl);

                // Restante do catálogo (não duplicar, enabled=false por padrão)
                foreach (var s in CveSourceCatalog.All)
                {
                    if (s.Name.Equals("NVD", StringComparison.OrdinalIgnoreCase) || s.Name.Equals("CIRCL", StringComparison.OrdinalIgnoreCase))
                        continue;
                    if (seen.Add(s.Name)) list.Add(new SourceView(s.Name, false, s.Url));
                }

                // Permitir habilitar fontes adicionais via env var OMAMA_ENABLE_SOURCES (lista de nomes separados por vírgula)
                var envEnable = Environment.GetEnvironmentVariable("OMAMA_ENABLE_SOURCES");
                if (!string.IsNullOrWhiteSpace(envEnable))
                {
                    var toEnable = envEnable
                        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                        .Select(x => x.Trim())
                        .ToHashSet(StringComparer.OrdinalIgnoreCase);
                    for (int i = 0; i < list.Count; i++)
                    {
                        if (toEnable.Contains(list[i].name))
                        {
                            list[i] = list[i] with { enabled = true };
                        }
                    }
                }

                if (asJson)
                {
                    var payload = new { providers = list };
                    Console.WriteLine(JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true }));
                }
                else
                {
                    Console.WriteLine("Fontes de CVE ativas:");
                    foreach (var s in list)
                    {
                        Console.WriteLine($"- {s.name}: {(s.enabled ? "habilitado" : "desabilitado")}");
                        Console.WriteLine($"  URL: {s.baseUrl}");
                    }
                }
                break;
            }

            // Subcomando: list cves (com filtros/paginação e formatos)
            if (args.Length > 1 && string.Equals(args[1], "cves", StringComparison.OrdinalIgnoreCase))
            {
                var opt = ParseOptions(args, 2);
                string? q = opt.GetValueOrDefault("q");
                DateTimeOffset? since = ParseDate(opt.GetValueOrDefault("since"));
                DateTimeOffset? until = ParseDate(opt.GetValueOrDefault("until"));
                var severities = opt.TryGetValue("severity", out var ssev) && !string.IsNullOrWhiteSpace(ssev)
                    ? ssev.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    : Array.Empty<string>();
                double? minScore = opt.TryGetValue("minScore", out var smin) && double.TryParse(smin, out var dmin) ? dmin : null;
                double? maxScore = opt.TryGetValue("maxScore", out var smax) && double.TryParse(smax, out var dmax) ? dmax : null;
                var sortBy = opt.GetValueOrDefault("sort") ?? "published";
                var descending = !opt.ContainsKey("asc");
                var page = opt.TryGetValue("page", out var sp) && int.TryParse(sp, out var ip) ? Math.Max(1, ip) : 1;
                var pageSize = opt.TryGetValue("pageSize", out var sps) && int.TryParse(sps, out var ips) ? Math.Clamp(ips, 1, 200) : 20;
                var format = (opt.GetValueOrDefault("format") ?? "text").ToLowerInvariant();

                string cacheDir = CachePaths.ResolveCacheDir();
                var query = new CveQueryService(cacheDir);
                var options = new CveQueryOptions(q, since, until, severities, minScore, maxScore, sortBy, descending, page, pageSize);
                var result = await query.ListSavedAsync(options);

                switch (format)
                {
                    case "json":
                        Console.WriteLine(JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
                        break;
                    case "md":
                        Console.WriteLine($"# CVEs (total: {result.Total}) page {result.Page} size {result.PageSize}");
                        foreach (var c in result.Items)
                        {
                            Console.WriteLine($"- **{c.Id}** | Score: {c.Score} | Severity: {c.Severity} | Published: {c.PublishedDate:yyyy-MM-dd}");
                            if (!string.IsNullOrWhiteSpace(c.Description))
                            {
                                var descText = c.Description.Replace('\n', ' ').Replace('\r', ' ').Trim();
                                Console.WriteLine($"  - {descText}");
                            }
                        }
                        break;
                    default:
                        Console.WriteLine($"CVEs (total: {result.Total}) page {result.Page} size {result.PageSize}");
                        foreach (var c in result.Items)
                        {
                            Console.WriteLine($"- {c.Id} | Score: {c.Score} | Severity: {c.Severity} | Published: {c.PublishedDate:yyyy-MM-dd}");
                            if (!string.IsNullOrWhiteSpace(c.Description))
                            {
                                var descText = c.Description.Replace('\n', ' ').Replace('\r', ' ').Trim();
                                Console.WriteLine($"  {descText}");
                            }
                        }
                        break;
                }
                break;
            }

            // Comportamento padrão do list (listar diretivas)
            var directives = directiveService.GetAllDirectives();
            foreach (var directive in directives)
            {
                Console.WriteLine($"Nome: {directive.Name}");
                Console.WriteLine($"Descrição: {directive.Description}");
                Console.WriteLine($"Valor: {directive.Value}");
                Console.WriteLine($"Criado em: {directive.CreatedAt}");
                if (directive.UpdatedAt.HasValue)
                    Console.WriteLine($"Atualizado em: {directive.UpdatedAt}");
                Console.WriteLine(new string('-', 50));
            }
            break;
        }
        case "stats":
        {
            if (args.Length > 1 && string.Equals(args[1], "source", StringComparison.OrdinalIgnoreCase))
            {
                var asJson = args.Skip(2).Any(a => string.Equals(a, "--json", StringComparison.OrdinalIgnoreCase));

                var options = Options.Create(new CveProviderOptions());
                var multiFactory = new MultiSourceProviderFactory(options);
                var providers = multiFactory.CreateNamed();

                var statsSvc = new CveSourceStatsService(providers);
                var counts = await statsSvc.GetCountsAsync();

                if (asJson)
                {
                    Console.WriteLine(JsonSerializer.Serialize(new { counts }, new JsonSerializerOptions { WriteIndented = true }));
                }
                else
                {
                    Console.WriteLine("Total de CVEs por fonte (quando suportado):");
                    foreach (var c in counts)
                    {
                        Console.WriteLine($"- {c.Name}: {(c.Total.HasValue ? c.Total.Value.ToString() : "desconhecido")}");
                    }
                }
                break;
            }
            Console.WriteLine("Subcomando desconhecido para stats. Use: stats source [--json]");
            break;
        }
        case "sync":
        {
            // subcomando: sync slow
            if (args.Length > 1 && string.Equals(args[1], "slow", StringComparison.OrdinalIgnoreCase))
            {
                var opts = ParseOptions(args, 2);
                var asJson = opts.ContainsKey("json");
                var force = opts.ContainsKey("force");
                var batch = opts.TryGetValue("batch", out var sb) && int.TryParse(sb, out var ib) ? Math.Max(1, ib) : 10;
                var maxPerHour = opts.TryGetValue("maxPerHour", out var sm) && int.TryParse(sm, out var im) ? Math.Max(1, im) : 500;
                // variável 'concurrency' removida

                string cacheDir = CachePaths.ResolveCacheDir();
                var configs = SourceConfigLoader.LoadDefault();
                var loader = new CveSourceLoader();
                var http = new HttpClient();
                var namedProviders = loader.LoadSources(http, configs);

                var results = new List<object>();
                var semaphore = new System.Threading.SemaphoreSlim(4); // max 4 concurrent
                var syncTasks = namedProviders.Select(async np =>
                {
                    await semaphore.WaitAsync();
                    try
                    {
                        Log($"Iniciando sync para fonte: {np.Name}");
                        var slow = new CveSlowSyncService(new[] { np }, cacheDir, maxPerHour, 1);
                        try
                        {
                            var stats = await slow.SyncLatestInterleavedAsync(batch, force);
                            Log($"Fonte: {np.Name} - Verificados: {stats.Scanned}, Existentes: {stats.Existed}, Salvos: {stats.Saved}, Erros: {stats.Errors}, Duração: {stats.Duration.TotalSeconds:F2}s");
                            return new {
                                Source = np.Name,
                                Scanned = stats.Scanned,
                                Existed = stats.Existed,
                                Saved = stats.Saved,
                                Errors = stats.Errors,
                                DurationMs = stats.Duration.TotalMilliseconds,
                                Metrics = stats.Metrics
                            };
                        }
                        catch (JsonException jex)
                        {
                            Log($"Erro de parsing JSON para fonte: {np.Name} - {jex.Message}");
                            return new {
                                Source = np.Name,
                                Scanned = 0,
                                Existed = 0,
                                Saved = 0,
                                Errors = 1,
                                DurationMs = 0.0,
                                Metrics = new Dictionary<string, object>()
                            };
                        }
                        catch (Exception ex)
                        {
                            Log($"Erro inesperado para fonte: {np.Name} - {ex.Message}");
                            return new {
                                Source = np.Name,
                                Scanned = 0,
                                Existed = 0,
                                Saved = 0,
                                Errors = 1,
                                DurationMs = 0.0,
                                Metrics = new Dictionary<string, object>()
                            };
                        }
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }).ToList();

                var syncResults = await Task.WhenAll(syncTasks);
                results.AddRange(syncResults);

                // Print telemetry at the end when in homol mode
                omama_cli.Services.CVE.TelemetryService.PrintSummary();

                if (asJson)
                {
                    Console.WriteLine(JsonSerializer.Serialize(new { results }, new JsonSerializerOptions { WriteIndented = true }));
                }
                else
                {
                    Console.WriteLine("Sync por fonte concluído:");
                    foreach (dynamic r in results)
                    {
                        Console.WriteLine($"Fonte: {r.Source}");
                        Console.WriteLine($"- Verificados: {r.Scanned}");
                        Console.WriteLine($"- Já existentes: {r.Existed}");
                        Console.WriteLine($"- Salvos agora: {r.Saved}");
                        Console.WriteLine($"- Erros: {r.Errors}");
                        Console.WriteLine(new string('-', 40));
                    }
                }
                break;
            }

            // sincronização padrão
            var opts2 = ParseOptions(args, 1);
            var asJson2 = opts2.ContainsKey("json");
            var force2 = opts2.ContainsKey("force");

            string? keyword = opts2.GetValueOrDefault("keyword");
            int limit = 50;
            if (opts2.TryGetValue("latest", out var latestStr) && int.TryParse(latestStr, out var latestVal))
            {
                limit = Math.Max(1, latestVal);
            }

            var cveOptions = Options.Create(new CveProviderOptions());
            var factory = new CveDataProviderFactory(cveOptions);
            var (provider, _) = factory.Create();

            string cacheDir2 = CachePaths.ResolveCacheDir();
            var syncService = new CveSyncService(provider, cacheDir2, cveOptions.Value.MaxParallelProcessing);

            var result = await syncService.SyncAsync(keyword, limit, force2);

            if (asJson2)
            {
                var payload = new
                {
                    mode = result.Mode,
                    scanned = result.Scanned,
                    existed = result.Existed,
                    saved = result.Saved,
                    errors = result.Errors
                };
                Console.WriteLine(JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true }));
            }
            else
            {
                Console.WriteLine("Sync concluído:");
                Console.WriteLine($"- Modo: {result.Mode}");
                Console.WriteLine($"- Verificados: {result.Scanned}");
                Console.WriteLine($"- Já existentes: {result.Existed}");
                Console.WriteLine($"- Salvos agora: {result.Saved}");
                if (result.Errors.Count > 0)
                {
                    Console.WriteLine($"- Erros: {result.Errors.Count}");
                }
            }
            break;
        }
        default:
            Console.WriteLine($"Comando desconhecido: {command}\n");
            PrintHelp();
            break;
    }
}
catch (Exception ex)
{
    await Console.Error.WriteLineAsync($"Erro: {ex.Message}\n");
    PrintHelp();
    Environment.ExitCode = 1;
}

static async Task TestProviderAsync(string providerName, int batch, bool useCurl, string method)
{
    Console.WriteLine($"[LOG] Testando provider: {providerName}");
    
    var configs = SourceConfigLoader.LoadDefault();
    var loader = new CveSourceLoader();
    var http = new HttpClient();
    var namedProviders = loader.LoadSources(http, configs);
    
    var provider = namedProviders.FirstOrDefault(p => 
        string.Equals(p.Name, providerName, StringComparison.OrdinalIgnoreCase));
    
    if (provider == null)
    {
        Console.WriteLine($"Provider '{providerName}' não encontrado.");
        Console.WriteLine($"Providers disponíveis: {string.Join(", ", namedProviders.Select(p => p.Name))}");
        return;
    }

    Console.WriteLine($"=== Testando Provider: {provider.Name} ===");
    
    // Testa curl se solicitado
    if (useCurl)
    {
        await TestWithCurlAsync(provider, method);
    }
    
    // Testa via provider
    await TestProviderDirectlyAsync(provider, batch);

    // Print telemetry summary when in homol mode
    if ((Environment.GetEnvironmentVariable("OMAMA_STAT") ?? "prod") == "homol")
    {
        TelemetryService.PrintSummary();
    }
}

static async Task TestWithCurlAsync(NamedCveProvider provider, string method)
{
    Console.WriteLine($"\n--- Teste CURL ({method}) ---");
    
    // Determina URL baseada no provider
    string testUrl = provider.Name.ToUpperInvariant() switch
    {
        "NVD" => CveSourceCatalog.NVD_URL + "?resultsPerPage=5",
        "CIRCL" => CveSourceCatalog.CIRCL_URL + "/last/5",
        "CVE.ORG" => CveSourceCatalog.CVEORG_URL + "/api/cve/search?q=*&limit=5",
        "CVEDETAILS" => CveSourceCatalog.CVEDETAILS_URL + "/json-feed.php?numrows=5",
        "VULNERS" => CveSourceCatalog.VULNERS_URL + "/api/v3/search/lucene/?query=*&limit=5",
    _ => string.Empty
    };
    
    Console.WriteLine($"[LOG] Testando URL: {testUrl}");
    
    var curlCommand = method == "POST" 
        ? $"curl -X POST -H 'Content-Type: application/json' -H 'User-Agent: OMAMA-CLI/1.0' '{testUrl}'"
        : $"curl -H 'User-Agent: OMAMA-CLI/1.0' '{testUrl}'";
    
    Console.WriteLine($"[LOG] Comando curl: {curlCommand}");
    
    var processInfo = new System.Diagnostics.ProcessStartInfo
    {
        FileName = "bash",
        Arguments = $"-c \"{curlCommand} | head -20\"",
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false
    };
    
    try
    {
        using var process = System.Diagnostics.Process.Start(processInfo);
        if (process != null)
        {
            var output = await process.StandardOutput.ReadToEndAsync();
            var error = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();
            
            Console.WriteLine($"Status: {process.ExitCode}");
            if (!string.IsNullOrEmpty(output))
            {
                Console.WriteLine($"Output:\n{output}");
            }
            if (!string.IsNullOrEmpty(error))
            {
                Console.WriteLine($"Error:\n{error}");
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Erro executando curl: {ex.Message}");
    }
}

static async Task TestProviderDirectlyAsync(NamedCveProvider provider, int batch)
{
    Console.WriteLine($"\n--- Teste Provider Direto ---");
    
    var sw = System.Diagnostics.Stopwatch.StartNew();
    
    try
    {
        // Teste GetLatestCvesAsync
        Console.WriteLine($"[LOG] Chamando GetLatestCvesAsync({batch})...");
        var latest = await provider.Provider.GetLatestCvesAsync(batch);
        var latestList = latest.ToList();
        
        Console.WriteLine($"GetLatestCves retornou: {latestList.Count} CVEs em {sw.ElapsedMilliseconds}ms");
        
        if (latestList.Count > 0)
        {
            var firstCve = latestList[0];
            Console.WriteLine($"Primeiro CVE: {firstCve.Id} - {firstCve.Description?[..Math.Min(100, firstCve.Description.Length)]}...");
            
            // Teste GetCveByIdAsync
            sw.Restart();
            Console.WriteLine($"[LOG] Testando GetCveByIdAsync com: {firstCve.Id}");
            var detailed = await provider.Provider.GetCveByIdAsync(firstCve.Id);
            
            Console.WriteLine($"GetCveById retornou: {(detailed != null ? "sucesso" : "falha")} em {sw.ElapsedMilliseconds}ms");
            
            if (detailed != null)
            {
                Console.WriteLine($"CVE detalhado: {detailed.Id} - Score: {detailed.Score} - Severity: {detailed.Severity}");
            }
        }
        
        // Teste contagem se disponível
        if (provider.Provider is IProvidesCveCount countProvider)
        {
            sw.Restart();
            Console.WriteLine("[LOG] Testando GetTotalCountAsync...");
            var total = await countProvider.GetTotalCountAsync();
            Console.WriteLine($"Total count: {total?.ToString() ?? "N/A"} em {sw.ElapsedMilliseconds}ms");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"ERRO testando provider: {ex.GetType().Name} - {ex.Message}");
        if (ex.InnerException != null)
        {
            Console.WriteLine($"Inner: {ex.InnerException.Message}");
        }
    }
    finally
    {
        sw.Stop();
    }
}
