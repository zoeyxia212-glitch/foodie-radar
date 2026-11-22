namespace FoodieRadar.Api.Models;

/// <param name="Key">Stable key stored on Restaurant.Cuisine</param>
/// <param name="Name">Display name</param>
/// <param name="Flag">Emoji flag</param>
/// <param name="OriginLabel">Label for "native" reviewers, e.g. "四川/中国"</param>
/// <param name="NativeOrigins">Reviewer origins that count as native for this cuisine</param>
public record CuisineInfo(string Key, string Name, string Flag, string OriginLabel, string[] NativeOrigins);

public static class CuisineCatalog
{
    public static readonly IReadOnlyList<CuisineInfo> Cuisines = new[]
    {
        new CuisineInfo("chinese", "中餐", "🇨🇳", "中国", new[] { "中国", "四川", "重庆", "广东", "香港", "台湾" }),
        new CuisineInfo("sichuan", "川菜", "🇨🇳", "四川/中国", new[] { "四川", "重庆", "中国" }),
        new CuisineInfo("canton", "粤菜", "🇨🇳", "广东/香港", new[] { "广东", "香港" }),
        new CuisineInfo("japan", "日料", "🇯🇵", "日本", new[] { "日本" }),
        new CuisineInfo("korea", "韩餐", "🇰🇷", "韩国", new[] { "韩国" }),
        new CuisineInfo("vietnam", "越南菜", "🇻🇳", "越南", new[] { "越南" }),
        new CuisineInfo("thai", "泰国菜", "🇹🇭", "泰国", new[] { "泰国" }),
        new CuisineInfo("malay", "马来菜", "🇲🇾", "马来西亚", new[] { "马来西亚" }),
    };

    public static readonly IReadOnlyList<string> Tastes = new[]
    {
        "麻辣", "鲜", "甜", "酸", "重口", "清淡", "锅气", "汤底浓"
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
        "中国", "四川", "重庆", "广东", "香港", "台湾", "日本", "韩国",
        "越南", "泰国", "马来西亚", "新西兰", "澳大利亚", "英国", "其他"
    };

    public static CuisineInfo? Find(string key) =>
        Cuisines.FirstOrDefault(c => c.Key == key);

    public static bool IsNative(string cuisineKey, string reviewerOrigin) =>
        Find(cuisineKey)?.NativeOrigins.Contains(reviewerOrigin) ?? false;
}
