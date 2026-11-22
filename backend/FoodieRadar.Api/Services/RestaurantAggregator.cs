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

        var parts = new List<string> { $"{reviews.Count} review{(reviews.Count == 1 ? "" : "s")}, average {overall:0.0}" };
        if (nativeAvg is not null)
            parts.Add($"{native.Count} from {cuisine?.OriginLabel ?? "native"} reviewers, average {nativeAvg:0.0}");
        if (r.MustOrder.Count > 0)
            parts.Add($"most recommended: {string.Join(", ", r.MustOrder.Take(3))}");
        if (r.Tastes.Count > 0)
            parts.Add($"taste: {string.Join(", ", r.Tastes)}");
        r.Summary = string.Join("; ", parts) + ".";

        string verdict;
        if (reviews.Count < 3)
            verdict = "Only a few reviews so far, so treat this as a rough guide.";
        else if (nativeAvg is not null && native.Count >= 2 && nativeAvg - overall >= 0.5)
            verdict = "Native reviewers rate it well above everyone else. Low scores are probably locals not used to the flavour, so go for it if you want the real thing.";
        else if (nativeAvg is not null && native.Count >= 2 && overall - nativeAvg >= 0.5)
            verdict = "Native reviewers rate it lower than everyone else. The flavours may be adapted to local tastes.";
        else if (overall >= 4.3)
            verdict = "Very well reviewed overall.";
        else if (overall < 3)
            verdict = "Mixed reviews overall. Go in with low expectations.";
        else
            verdict = "Decent reviews. Stick to the recommended dishes.";

        if (r.Avoid.Count > 0)
            verdict += $" Skip: {string.Join(", ", r.Avoid)}.";

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
