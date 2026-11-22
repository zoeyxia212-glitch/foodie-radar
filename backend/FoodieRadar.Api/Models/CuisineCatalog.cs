namespace FoodieRadar.Api.Models;

/// <param name="Key">Stable key stored on Restaurant.Cuisine</param>
/// <param name="Name">Display name</param>
/// <param name="Flag">Emoji flag</param>
/// <param name="OriginLabel">Label for "native" reviewers, e.g. "Sichuan/China"</param>
/// <param name="NativeOrigins">Reviewer origins that count as native for this cuisine</param>
public record CuisineInfo(string Key, string Name, string Flag, string OriginLabel, string[] NativeOrigins);

public static class CuisineCatalog
{
    public static readonly IReadOnlyList<CuisineInfo> Cuisines = new[]
    {
        new CuisineInfo("chinese", "Chinese", "🇨🇳", "China", new[] { "China", "Sichuan", "Chongqing", "Guangdong", "Hong Kong", "Taiwan" }),
        new CuisineInfo("sichuan", "Sichuan", "🇨🇳", "Sichuan/China", new[] { "Sichuan", "Chongqing", "China" }),
        new CuisineInfo("canton", "Cantonese", "🇨🇳", "Guangdong/HK", new[] { "Guangdong", "Hong Kong" }),
        new CuisineInfo("japan", "Japanese", "🇯🇵", "Japan", new[] { "Japan" }),
        new CuisineInfo("korea", "Korean", "🇰🇷", "Korea", new[] { "Korea" }),
        new CuisineInfo("vietnam", "Vietnamese", "🇻🇳", "Vietnam", new[] { "Vietnam" }),
        new CuisineInfo("thai", "Thai", "🇹🇭", "Thailand", new[] { "Thailand" }),
        new CuisineInfo("malay", "Malaysian", "🇲🇾", "Malaysia", new[] { "Malaysia" }),
    };

    public static readonly IReadOnlyList<string> Tastes = new[]
    {
        "Mala", "Fresh", "Sweet", "Sour", "Bold", "Light", "Wok hei", "Rich broth"
    };

    public static readonly IReadOnlyList<string> Areas = new[]
    {
        "North Shore", "Central Auckland", "West Auckland", "East Auckland", "South Auckland"
    };

    /// <summary>
    /// Rough region bucketing for Auckland by coordinates.
    /// Good enough for filtering; refine with suburb boundaries later.
    /// </summary>
    public static string RegionFor(double lat, double lon)
    {
        if (lat > -36.83) return "North Shore";       // north of the Waitematā Harbour
        if (lon < 174.68) return "West Auckland";
        if (lat < -36.93) return "South Auckland";
        if (lon > 174.82) return "East Auckland";
        return "Central Auckland";
    }

    public static readonly IReadOnlyList<string> Origins = new[]
    {
        "China", "Sichuan", "Chongqing", "Guangdong", "Hong Kong", "Taiwan", "Japan", "Korea",
        "Vietnam", "Thailand", "Malaysia", "New Zealand", "Australia", "UK", "Other"
    };

    public static CuisineInfo? Find(string key) =>
        Cuisines.FirstOrDefault(c => c.Key == key);

    public static bool IsNative(string cuisineKey, string reviewerOrigin) =>
        Find(cuisineKey)?.NativeOrigins.Contains(reviewerOrigin) ?? false;
}
