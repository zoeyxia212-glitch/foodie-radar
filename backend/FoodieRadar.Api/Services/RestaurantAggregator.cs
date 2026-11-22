using FoodieRadar.Api.Models;

namespace FoodieRadar.Api.Services;

/// <summary>
/// Rebuilds a restaurant's profile (must-order, avoid, tastes, spice, price, summary, verdict)
/// from its reviews. Rule-based for now; an LLM can replace the summary/verdict text later.
/// Requires <see cref="Restaurant.Reviews"/> to be loaded.
/// </summary>
public static class RestaurantAggregator
{
    public static void Recompute(Restaurant r)
    {
        if (r.IsCurated || r.Reviews.Count == 0) return;

        var reviews = r.Reviews;

        // Dishes: a dish is "must order" when recommended more often than flagged, and vice versa.
        var rec = Count(reviews.SelectMany(v => v.RecommendedDishes));
        var bad = Count(reviews.SelectMany(v => v.AvoidDishes));

        r.MustOrder = rec
            .Where(kv => kv.Value > bad.GetValueOrDefault(kv.Key))
            .OrderByDescending(kv => kv.Value)
            .Take(5).Select(kv => kv.Key).ToList();

        r.Avoid = bad
            .Where(kv => kv.Value > rec.GetValueOrDefault(kv.Key))
            .OrderByDescending(kv => kv.Value)
            .Take(3).Select(kv => kv.Key).ToList();

        // Tastes: keep tags picked by at least 30% of reviewers who picked any.
        var tagged = reviews.Where(v => v.Tastes.Count > 0).ToList();
        if (tagged.Count > 0)
        {
            var threshold = Math.Max(1, (int)Math.Ceiling(tagged.Count * 0.3));
            r.Tastes = Count(tagged.SelectMany(v => v.Tastes))
                .Where(kv => kv.Value >= threshold)
                .OrderByDescending(kv => kv.Value)
                .Take(4).Select(kv => kv.Key).ToList();
        }

        var spice = reviews.Where(v => v.SpiceRating.HasValue).Select(v => v.SpiceRating!.Value).ToList();
        if (spice.Count > 0) r.SpiceLevel = RoundAvg(spice);

        var price = reviews.Where(v => v.PriceLevel.HasValue).Select(v => v.PriceLevel!.Value).ToList();
        if (price.Count > 0) r.PriceLevel = RoundAvg(price);

        BuildText(r);
    }

    private static void BuildText(Restaurant r)
    {
        var reviews = r.Reviews;
        var cuisine = CuisineCatalog.Find(r.Cuisine);
        var native = reviews.Where(v => CuisineCatalog.IsNative(r.Cuisine, v.ReviewerOrigin)).ToList();
        var overall = reviews.Average(v => v.Rating);
        double? nativeAvg = native.Count > 0 ? native.Average(v => v.Rating) : null;

        var parts = new List<string> { $"共 {reviews.Count} 条评价，平均 {overall:0.0} 分" };
        if (nativeAvg is not null)
            parts.Add($"{cuisine?.OriginLabel ?? "同乡"}评价者 {native.Count} 条，平均 {nativeAvg:0.0} 分");
        if (r.MustOrder.Count > 0)
            parts.Add($"最常被推荐：{string.Join("、", r.MustOrder.Take(3))}");
        if (r.Tastes.Count > 0)
            parts.Add($"口味：{string.Join("、", r.Tastes)}");
        r.Summary = string.Join("；", parts) + "。";

        string verdict;
        if (reviews.Count < 3)
            verdict = "评价还比较少，结论仅供参考。";
        else if (nativeAvg is not null && native.Count >= 2 && nativeAvg - overall >= 0.5)
            verdict = "同乡评价明显高于整体，差评多半是本地人不习惯这个口味，想吃正宗的可以放心。";
        else if (nativeAvg is not null && native.Count >= 2 && overall - nativeAvg >= 0.5)
            verdict = "同乡评价者打分偏低，口味可能比较本地化，追求正宗的话要有心理准备。";
        else if (overall >= 4.3)
            verdict = "整体口碑很好。";
        else if (overall < 3)
            verdict = "整体口碑一般，谨慎尝试。";
        else
            verdict = "口碑中等，可以按推荐菜点。";

        if (r.Avoid.Count > 0)
            verdict += $"可以跳过：{string.Join("、", r.Avoid)}。";

        r.AiVerdict = verdict;
    }

    private static int RoundAvg(List<int> values) =>
        (int)Math.Round(values.Average(), MidpointRounding.AwayFromZero);

    private static Dictionary<string, int> Count(IEnumerable<string> items)
    {
        var d = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var raw in items)
        {
            var s = raw.Trim();
            if (s.Length == 0) continue;
            d[s] = d.GetValueOrDefault(s) + 1;
        }
        return d;
    }
}
