using System.Net.Http.Json;

namespace GW2ClarityBlish.Services;

public record SkillIcon(int Id, string Icon);

public class BuffCatalogService
{
    public const string UnknownIconUrl = "unknown-buff-placeholder";

    private readonly HttpClient _http;
    private readonly Dictionary<uint, string> _cache = new();

    public BuffCatalogService(HttpClient http)
    {
        _http = http;
        _http.Timeout = TimeSpan.FromSeconds(10);
    }

    public async Task<string> GetIconUrlAsync(uint buffId)
    {
        if (_cache.TryGetValue(buffId, out var cached))
            return cached;

        try
        {
            var response = await _http.GetAsync($"https://api.guildwars2.com/v2/skills?ids={buffId}");
            if (!response.IsSuccessStatusCode)
                return UnknownIconUrl;

            var skills = await response.Content.ReadFromJsonAsync<List<SkillIcon>>();
            var icon = skills?.FirstOrDefault()?.Icon ?? UnknownIconUrl;

            _cache[buffId] = icon;
            return icon;
        }
        catch (HttpRequestException)
        {
            return UnknownIconUrl;
        }
        catch (TaskCanceledException)
        {
            return UnknownIconUrl;
        }
    }
}
