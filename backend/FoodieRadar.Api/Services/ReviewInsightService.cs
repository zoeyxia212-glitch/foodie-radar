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
        "too spicy", "too salty", "too sweet", "too oily", "bland", "not spicy",
        "authentic", "wok hei", "portion", "queue", "value for money", "broth",
        "numbing", "fresh", "greasy", "overpriced", "generous"
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
