using System.Text.Json;
using GW2ClarityBlish.Models;

namespace GW2ClarityBlish.Services;

public class StyleStore
{
    private readonly string _path;
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public StyleStore(string storageDirectory)
    {
        Directory.CreateDirectory(storageDirectory);
        _path = Path.Combine(storageDirectory, "styles.json");
    }

    public void Save(IReadOnlyList<Style> styles)
    {
        File.WriteAllText(_path, JsonSerializer.Serialize(styles, JsonOptions));
    }

    public List<Style> Load()
    {
        if (!File.Exists(_path))
            return new List<Style>();

        try
        {
            var json = File.ReadAllText(_path);
            return JsonSerializer.Deserialize<List<Style>>(json) ?? new List<Style>();
        }
        catch (JsonException)
        {
            var backupPath = $"{_path}.corrupt-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}";
            File.Move(_path, backupPath);
            return new List<Style>();
        }
    }
}
