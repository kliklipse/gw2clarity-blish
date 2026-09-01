using System.Text.Json;
using GW2ClarityBlish.Models;

namespace GW2ClarityBlish.Services;

public class LayoutStore
{
    private readonly string _path;
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public LayoutStore(string storageDirectory)
    {
        Directory.CreateDirectory(storageDirectory);
        _path = Path.Combine(storageDirectory, "layouts.json");
    }

    public void Save(IReadOnlyList<Layout> layouts)
    {
        File.WriteAllText(_path, JsonSerializer.Serialize(layouts, JsonOptions));
    }

    public List<Layout> Load()
    {
        if (!File.Exists(_path))
            return new List<Layout>();

        try
        {
            var json = File.ReadAllText(_path);
            return JsonSerializer.Deserialize<List<Layout>>(json) ?? new List<Layout>();
        }
        catch (JsonException)
        {
            var backupPath = $"{_path}.corrupt-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}";
            File.Move(_path, backupPath);
            return new List<Layout>();
        }
    }
}
