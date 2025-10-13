using omama_cli.Services;
using omama_cli.Services.CVE;
using omama_cli.Models;
using System.Text.Json;
using Microsoft.Extensions.Options;

static void PrintHelp()
{
    Console.WriteLine("Omama CLI - Gerenciador de Diretivas");
    Console.WriteLine();
    Console.WriteLine("Uso:");
    Console.WriteLine("  omama-cli add --name <nome> --description <desc> --value <valor>");
    Console.WriteLine("  omama-cli list");
    Console.WriteLine("  omama-cli list source [--json]");
    Console.WriteLine("  omama-cli update --name <nome> --value <novoValor>");
    Console.WriteLine("  omama-cli delete --name <nome>");
    Console.WriteLine("  omama-cli sync [--keyword <kw> | --latest <n>] [--force] [--json]");
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

                // Recupera fontes a partir das opções (defaults por enquanto)
                var cveOptions = new CveProviderOptions();
                var sources = new[]
                {
                    new { name = "NVD", enabled = cveOptions.EnableNvd, baseUrl = cveOptions.NvdBaseUrl },
                    new { name = "CIRCL", enabled = cveOptions.EnableCircl, baseUrl = cveOptions.CirclBaseUrl },
                };

                if (asJson)
                {
                    var payload = new { providers = sources };
                    Console.WriteLine(JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true }));
                }
                else
                {
                    Console.WriteLine("Fontes de CVE ativas:");
                    foreach (var s in sources)
                    {
                        Console.WriteLine($"- {s.name}: {(s.enabled ? "habilitado" : "desabilitado")}");
                        Console.WriteLine($"  URL: {s.baseUrl}");
                    }
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
        case "update":
        {
            var opts = ParseOptions(args, 1);
            var name = opts.GetValueOrDefault("name") ?? throw new ArgumentException("--name é obrigatório");
            var value = opts.GetValueOrDefault("value") ?? throw new ArgumentException("--value é obrigatório");
            directiveService.UpdateDirective(name, value);
            Console.WriteLine($"Diretiva '{name}' atualizada com sucesso!");
            break;
        }
        case "delete":
        {
            var opts = ParseOptions(args, 1);
            var name = opts.GetValueOrDefault("name") ?? throw new ArgumentException("--name é obrigatório");
            directiveService.DeleteDirective(name);
            Console.WriteLine($"Diretiva '{name}' removida com sucesso!");
            break;
        }
        case "sync":
        {
        // Options: --keyword <kw> | --latest <n> (default 50), [--json]
    var opts = ParseOptions(args, 1);
    var asJson = opts.ContainsKey("json");
    var force = opts.ContainsKey("force");

    string? keyword = opts.GetValueOrDefault("keyword");
        int limit = 50;
        if (opts.TryGetValue("latest", out var latestStr) && int.TryParse(latestStr, out var latestVal))
        {
            limit = Math.Max(1, latestVal);
        }

        var cveOptions = Options.Create(new CveProviderOptions());
        var factory = new CveDataProviderFactory(cveOptions);
        var (provider, _) = factory.Create();

        string cacheDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "omama-cli", "cache");
        var syncService = new CveSyncService(provider, cacheDir, cveOptions.Value.MaxParallelProcessing);

    var result = await syncService.SyncAsync(keyword, limit, force);

        if (asJson)
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
