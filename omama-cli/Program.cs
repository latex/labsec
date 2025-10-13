using omama_cli.Services;
using omama_cli.Services.CVE;
using omama_cli.Models;
using System.Text.Json;
using Microsoft.Extensions.Options;
using System.Globalization;

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
    switch (command)
    {
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

                string cacheDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "omama-cli", "cache");
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
                var concurrency = opts.TryGetValue("concurrency", out var sc) && int.TryParse(sc, out var ic) ? Math.Max(1, ic) : 4;

                var options = Options.Create(new CveProviderOptions());
                var multiFactory = new MultiSourceProviderFactory(options);
                var named = multiFactory.CreateNamed();

                string cacheDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "omama-cli", "cache");
                var slow = new CveSlowSyncService(named, cacheDir, maxPerHour, concurrency);
                var stats = await slow.SyncLatestInterleavedAsync(batch, force);

                if (asJson)
                {
                    Console.WriteLine(JsonSerializer.Serialize(stats, new JsonSerializerOptions { WriteIndented = true }));
                }
                else
                {
                    Console.WriteLine("Slow sync concluído:");
                    Console.WriteLine($"- Verificados: {stats.Scanned}");
                    Console.WriteLine($"- Já existentes: {stats.Existed}");
                    Console.WriteLine($"- Salvos agora: {stats.Saved}");
                    Console.WriteLine($"- Erros: {stats.Errors}");
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

            string cacheDir2 = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "omama-cli", "cache");
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
