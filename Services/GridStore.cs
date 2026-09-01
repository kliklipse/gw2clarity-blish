using System.Text.Json;
using GW2ClarityBlish.Models;

namespace GW2ClarityBlish.Services;

public class GridStore
{
    private readonly string _path;
    // IncludeFields = true est indispensable ici : Vector2/Vector4 (System.Numerics) et les
    // tuples (int X, int Y) exposent leurs donnees comme des CHAMPS publics (X/Y/Z/W, Item1/Item2),
    // pas des proprietes - System.Text.Json ignore les champs par defaut. Sans ce flag, Spacing,
    // Offset, Position, Tint, Border, Glow etc. se serialisaient silencieusement en "{}" (tout a
    // zero) a chaque sauvegarde, constate en test reel le 2026-09-02 (grille invisible : taille
    // 1x1px, tint totalement transparent).
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true, IncludeFields = true };

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
            return JsonSerializer.Deserialize<List<Grid>>(json, JsonOptions) ?? new List<Grid>();
        }
        catch (JsonException)
        {
            var backupPath = $"{_path}.corrupt-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}";
            File.Move(_path, backupPath);
            return new List<Grid>();
        }
    }
}
