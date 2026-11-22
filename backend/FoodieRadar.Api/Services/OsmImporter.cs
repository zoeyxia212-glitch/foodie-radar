using System.Net.Http.Json;
using FoodieRadar.Api.Data;
using FoodieRadar.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace FoodieRadar.Api.Services;

/// <summary>
/// Imports Asian restaurants in Auckland from OpenStreetMap via the free Overpass API.
/// OSM data is © OpenStreetMap contributors, licensed under ODbL — keep the attribution
/// shown in the frontend footer.
/// </summary>
public class OsmImporter(HttpClient http, AppDbContext db, ILogger<OsmImporter> log)
{
    public const string Endpoint = "https://overpass-api.de/api/interpreter";

    // Auckland urban area bounding box: south, west, north, east
    private const string AucklandBbox = "-37.10,174.55,-36.65,175.00";

    private const string CuisineRegex =
        "chinese|sichuan|szechuan|cantonese|dim_sum|hong_kong|japanese|sushi|ramen|udon|izakaya|" +
        "korean|vietnamese|pho|thai|malaysian|malay|singaporean";

    // Order matters: more specific tags first.
    private static readonly (string Key, string[] OsmTags)[] CuisineMap =
    {
        ("sichuan", new[] { "sichuan", "szechuan" }),
        ("canton", new[] { "cantonese", "dim_sum", "hong_kong" }),
        ("chinese", new[] { "chinese" }),
        ("japan", new[] { "japanese", "sushi", "ramen", "udon", "izakaya" }),
        ("korea", new[] { "korean" }),
        ("vietnam", new[] { "vietnamese", "pho" }),
        ("thai", new[] { "thai" }),
        ("malay", new[] { "malaysian", "malay", "singaporean" }),
    };

    public record ImportResult(int Fetched, int Created, int Updated, int Skipped, int DemoRemoved);

    public async Task<ImportResult> ImportAucklandAsync(bool removeDemo, CancellationToken ct = default)
    {
        var query = $"""
            [out:json][timeout:120];
            nwr["amenity"~"^(restaurant|fast_food|cafe|food_court)$"]["cuisine"~"{CuisineRegex}",i]({AucklandBbox});
            out center tags;
            """;

        log.LogInformation("Querying Overpass API…");
        using var content = new FormUrlEncodedContent(new Dictionary<string, string> { ["data"] = query });
        using var res = await http.PostAsync(Endpoint, content, ct);
        res.EnsureSuccessStatusCode();

        var body = await res.Content.ReadFromJsonAsync<OverpassResponse>(cancellationToken: ct)
                   ?? new OverpassResponse(new List<OverpassElement>());

        var existing = await db.Restaurants
            .Where(r => r.OsmKey != null)
            .ToDictionaryAsync(r => r.OsmKey!, ct);

        int created = 0, updated = 0, skipped = 0;

        foreach (var el in body.Elements)
        {
            var tags = el.Tags ?? new Dictionary<string, string>();
            var lat = el.Lat ?? el.Center?.Lat;
            var lon = el.Lon ?? el.Center?.Lon;
            var name = tags.GetValueOrDefault("name");
            var cuisine = MapCuisine(tags.GetValueOrDefault("cuisine"));

            if (string.IsNullOrWhiteSpace(name) || lat is null || lon is null || cuisine is null)
            {
                skipped++;
                continue;
            }

            var key = $"{el.Type}/{el.Id}";
            if (!existing.TryGetValue(key, out var r))
            {
                r = new Restaurant { OsmKey = key };
                db.Restaurants.Add(r);
                existing[key] = r;
                created++;
            }
            else
            {
                updated++;
            }

            // Only overwrite fields that come from OSM; keep reviews, summaries and tags.
            r.Name = tags.GetValueOrDefault("name:zh") ?? name;
            r.EnglishName = tags.GetValueOrDefault("name:en") ?? name;
            r.Cuisine = cuisine;
            r.Latitude = lat;
            r.Longitude = lon;
            r.Area = CuisineCatalog.RegionFor(lat.Value, lon.Value);
            r.Suburb = tags.GetValueOrDefault("addr:suburb") ?? tags.GetValueOrDefault("addr:city") ?? "";
            r.Address = BuildAddress(tags);
            r.Website = tags.GetValueOrDefault("website") ?? tags.GetValueOrDefault("contact:website");
            r.Phone = tags.GetValueOrDefault("phone") ?? tags.GetValueOrDefault("contact:phone");
            r.OpeningHours = tags.GetValueOrDefault("opening_hours");
        }

        int demoRemoved = 0;
        if (removeDemo)
        {
            var demo = await db.Restaurants.Where(r => r.IsDemo).ToListAsync(ct);
            db.Restaurants.RemoveRange(demo);
            demoRemoved = demo.Count;
        }

        await db.SaveChangesAsync(ct);

        var result = new ImportResult(body.Elements.Count, created, updated, skipped, demoRemoved);
        log.LogInformation("OSM import done: {@Result}", result);
        return result;
    }

    private static string? MapCuisine(string? osmCuisine)
    {
        if (string.IsNullOrWhiteSpace(osmCuisine)) return null;
        var values = osmCuisine.ToLowerInvariant().Split(';', StringSplitOptions.TrimEntries);
        foreach (var (key, osmTags) in CuisineMap)
            if (values.Any(v => osmTags.Contains(v))) return key;
        return null;
    }

    private static string BuildAddress(Dictionary<string, string> tags)
    {
        var street = string.Join(" ", new[]
        {
            tags.GetValueOrDefault("addr:housenumber"),
            tags.GetValueOrDefault("addr:street")
        }.Where(s => !string.IsNullOrWhiteSpace(s)));

        return string.Join(", ", new[]
        {
            street,
            tags.GetValueOrDefault("addr:suburb"),
            tags.GetValueOrDefault("addr:city")
        }.Where(s => !string.IsNullOrWhiteSpace(s)));
    }

    private record OverpassResponse(List<OverpassElement> Elements);

    private record OverpassElement(
        string Type,
        long Id,
        double? Lat,
        double? Lon,
        OverpassCenter? Center,
        Dictionary<string, string>? Tags);

    private record OverpassCenter(double Lat, double Lon);
}
