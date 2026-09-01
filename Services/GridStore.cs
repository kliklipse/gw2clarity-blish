using System.Text.Json;
using GW2ClarityBlish.Models;

namespace GW2ClarityBlish.Services;

public class GridStore
{
    private readonly string _path;
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public GridStore(string storageDirectory)
    {
        Directory.CreateDirectory(storageDirectory);
        _path = Path.Combine(storageDirectory, "grids.json");
    }

    public void Save(IReadOnlyList<Grid> grids)
    {
        File.WriteAllText(_path, JsonSerializer.Serialize(grids, JsonOptions));
    }

    public List<Grid> Load()
    {
        if (!File.Exists(_path))
            return new List<Grid>();

        try
        {
            var json = File.ReadAllText(_path);
            return JsonSerializer.Deserialize<List<Grid>>(json) ?? new List<Grid>();
        }
        catch (JsonException)
        {
            var backupPath = $"{_path}.corrupt-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}";
            File.Move(_path, backupPath);
            return new List<Grid>();
        }
    }
}
