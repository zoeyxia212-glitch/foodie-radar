using FoodieRadar.Api.Models;
using FoodieRadar.Api.Services;

namespace FoodieRadar.Api.Tests.Unit;

public class RestaurantAggregatorTests
{
    private static Review Rev(int rating, string origin,
        string[]? rec = null, string[]? avoid = null, string[]? tastes = null, int? spice = null, int? price = null) => new()
    {
        Rating = rating,
        ReviewerOrigin = origin,
        RecommendedDishes = (rec ?? Array.Empty<string>()).ToList(),
        AvoidDishes = (avoid ?? Array.Empty<string>()).ToList(),
        Tastes = (tastes ?? Array.Empty<string>()).ToList(),
        SpiceRating = spice,
        PriceLevel = price,
    };

    private static Restaurant SichuanWithReviews() => new()
    {
        Cuisine = "sichuan",
        Reviews =
        {
            Rev(5, "Sichuan", rec: new[] { "Fish", "Chicken" }, tastes: new[] { "Mala" }, spice: 4, price: 2),
            Rev(5, "China", rec: new[] { "fish" }, avoid: new[] { "Kung pao" }, tastes: new[] { "Mala", "Bold" }, spice: 5, price: 2),
            Rev(2, "New Zealand", avoid: new[] { "Fish" }, tastes: new[] { "Bold" }, spice: 5, price: 3),
            Rev(2, "New Zealand"),
        }
    };

    [Fact]
    public void MustOrder_lists_dishes_recommended_more_than_flagged_most_popular_first()
    {
        var r = SichuanWithReviews();
        RestaurantAggregator.Recompute(r);

        // "Fish" recommended twice (case-insensitive), flagged once; "Chicken" recommended once.
        Assert.Equal(new[] { "Fish", "Chicken" }, r.MustOrder);
    }

    [Fact]
    public void Avoid_lists_dishes_flagged_more_than_recommended()
    {
        var r = SichuanWithReviews();
        RestaurantAggregator.Recompute(r);

        Assert.Equal(new[] { "Kung pao" }, r.Avoid);
    }

    [Fact]
    public void Tastes_spice_and_price_come_from_reviewer_input()
    {
        var r = SichuanWithReviews();
        RestaurantAggregator.Recompute(r);

        Assert.Equal(new[] { "Mala", "Bold" }, r.Tastes);
        Assert.Equal(5, r.SpiceLevel);  // (4 + 5 + 5) / 3 = 4.67 -> 5
        Assert.Equal(2, r.PriceLevel);  // (2 + 2 + 3) / 3 = 2.33 -> 2
    }

    [Fact]
    public void Verdict_says_native_reviewers_rate_it_higher_and_lists_dishes_to_skip()
    {
        var r = SichuanWithReviews();
        RestaurantAggregator.Recompute(r);

        // Native (Sichuan + China) average 5.0 vs overall 3.5
        Assert.StartsWith("Native reviewers rate it well above", r.AiVerdict);
        Assert.Contains("Skip: Kung pao.", r.AiVerdict);
        Assert.StartsWith("4 reviews, average 3.5", r.Summary);
    }

    [Fact]
    public void Few_reviews_get_a_cautious_verdict()
    {
        var r = new Restaurant { Cuisine = "thai", Reviews = { Rev(5, "Thailand") } };
        RestaurantAggregator.Recompute(r);

        Assert.StartsWith("Only a few reviews so far", r.AiVerdict);
    }

    [Fact]
    public void Curated_restaurants_are_not_overwritten()
    {
        var r = SichuanWithReviews();
        r.IsCurated = true;
        r.MustOrder = new List<string> { "Hand-picked dish" };
        r.AiVerdict = "Hand-written";

        RestaurantAggregator.Recompute(r);

        Assert.Equal(new[] { "Hand-picked dish" }, r.MustOrder);
        Assert.Equal("Hand-written", r.AiVerdict);
    }

    [Fact]
    public void No_reviews_leaves_restaurant_unchanged()
    {
        var r = new Restaurant { Cuisine = "korea", Summary = "" };
        RestaurantAggregator.Recompute(r);

        Assert.Equal("", r.Summary);
        Assert.Empty(r.MustOrder);
    }
}
