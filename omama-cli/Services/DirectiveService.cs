using omama_cli.Models;
using System.Text.Json;

namespace omama_cli.Services;

public class DirectiveService
{
    private readonly string _storagePath;
    private List<Directive> _directives;

    public DirectiveService(string storagePath = "directives.json")
    {
        _storagePath = storagePath;
        _directives = LoadDirectives();
    }

    private List<Directive> LoadDirectives()
    {
        if (!File.Exists(_storagePath))
            return new List<Directive>();

        var json = File.ReadAllText(_storagePath);
        return JsonSerializer.Deserialize<List<Directive>>(json) ?? new List<Directive>();
    }

    private void SaveDirectives()
    {
        var json = JsonSerializer.Serialize(_directives, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_storagePath, json);
    }

    public void AddDirective(string name, string description, string value)
    {
        var directive = new Directive
        {
            Name = name,
            Description = description,
            Value = value,
            CreatedAt = DateTime.Now
        };

        _directives.Add(directive);
        SaveDirectives();
    }

    public void UpdateDirective(string name, string newValue)
    {
        var directive = _directives.FirstOrDefault(d => d.Name == name && d.IsActive);
        if (directive != null)
        {
            directive.Value = newValue;
            directive.UpdatedAt = DateTime.Now;
            SaveDirectives();
        }
    }

    public void DeleteDirective(string name)
    {
        var directive = _directives.FirstOrDefault(d => d.Name == name && d.IsActive);
        if (directive != null)
        {
            directive.IsActive = false;
            directive.UpdatedAt = DateTime.Now;
            SaveDirectives();
        }
    }

    public Directive? GetDirective(string name)
    {
        return _directives.FirstOrDefault(d => d.Name == name && d.IsActive);
    }

    public IEnumerable<Directive> GetAllDirectives()
    {
        return _directives.Where(d => d.IsActive).ToList();
    }
}