using FoodieRadar.Api.Models;

namespace FoodieRadar.Api.Services;

/// <summary>
/// Extracts key phrases (dishes, taste notes) from a review.
/// The current implementation is rule-based; swap in an LLM-backed
/// implementation later without touching controllers.
/// </summary>
public interface IReviewInsightService
{
    List<string> ExtractHighlights(Restaurant restaurant, string text, IEnumerable<string> extraDishes);
}

public class KeywordInsightService : IReviewInsightService
{
    private static readonly string[] TastePhrases =
    {
        "太辣", "太咸", "太甜", "太油", "太淡", "偏甜", "偏咸", "不辣", "够麻", "够辣",
        "正宗", "锅气", "份量", "排队", "性价比", "汤底",
        "too spicy", "too salty", "too sweet", "oily", "authentic", "portion", "value"
    };

    public List<string> ExtractHighlights(Restaurant restaurant, string text, IEnumerable<string> extraDishes)
    {
        var dishes = restaurant.MustOrder.Concat(restaurant.Avoid).Concat(extraDishes);

        return dishes.Concat(TastePhrases)
            .Where(p => !string.IsNullOrWhiteSpace(p) && text.Contains(p, StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
