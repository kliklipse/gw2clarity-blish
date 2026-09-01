using System.Text.Json;
using GW2ClarityBlish.Models;

namespace GW2ClarityBlish.Services;

public class LayoutStore
{
    private readonly string _path;
    // Voir GridStore.JsonOptions pour le detail : IncludeFields necessaire des qu'un modele
    // touche System.Numerics (Vector2/Vector4) ou un tuple - par coherence avec les stores
    // freres, meme si Layout n'en a pas directement aujourd'hui.
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true, IncludeFields = true };

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
            return JsonSerializer.Deserialize<List<Layout>>(json, JsonOptions) ?? new List<Layout>();
        }
        catch (JsonException)
        {
            var backupPath = $"{_path}.corrupt-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}";
            File.Move(_path, backupPath);
            return new List<Layout>();
        }
    }
}
