using FoodieRadar.Api.Dtos;
using FoodieRadar.Api.Models;
using FoodieRadar.Api.Services;

namespace FoodieRadar.Api.Tests.Unit;

public class CuisineCatalogTests
{
    [Theory]
    [InlineData("sichuan", "Sichuan", true)]
    [InlineData("sichuan", "China", true)]
    [InlineData("sichuan", "New Zealand", false)]
    [InlineData("japan", "Japan", true)]
    [InlineData("japan", "Korea", false)]
    [InlineData("unknown-cuisine", "China", false)]
    public void IsNative_matches_reviewer_origin_to_cuisine(string cuisine, string origin, bool expected) =>
        Assert.Equal(expected, CuisineCatalog.IsNative(cuisine, origin));

    [Theory]
    [InlineData(-36.7880, 174.7720, "North Shore")]      // Takapuna
    [InlineData(-36.8485, 174.7622, "Central Auckland")] // Sky Tower
    [InlineData(-36.8770, 174.6300, "West Auckland")]    // Henderson
    [InlineData(-36.9900, 174.8800, "South Auckland")]   // Manukau
    [InlineData(-36.8800, 174.9000, "East Auckland")]    // Pakuranga
    public void RegionFor_buckets_coordinates_into_auckland_regions(double lat, double lon, string expected) =>
        Assert.Equal(expected, CuisineCatalog.RegionFor(lat, lon));

    [Fact]
    public void Every_cuisine_has_native_origins_that_exist_in_the_origin_list()
    {
        foreach (var c in CuisineCatalog.Cuisines)
            Assert.All(c.NativeOrigins, o => Assert.Contains(o, CuisineCatalog.Origins));
    }
}

public class GeoTests
{
    [Fact]
    public void Sky_Tower_to_Takapuna_is_about_7_km()
    {
        var km = Geo.DistanceKm(-36.8485, 174.7622, -36.7880, 174.7720);
        Assert.InRange(km, 6.0, 8.0);
    }

    [Fact]
    public void Same_point_is_zero_km() =>
        Assert.Equal(0, Geo.DistanceKm(-36.85, 174.76, -36.85, 174.76), precision: 6);
}

public class KeywordInsightServiceTests
{
    [Fact]
    public void Highlights_known_dishes_and_taste_phrases_case_insensitively()
    {
        var restaurant = new Restaurant { MustOrder = { "Laksa" }, Avoid = { "Roti" } };
        var text = "The LAKSA was great but way too salty. Roti was fine.";

        var hl = new KeywordInsightService().ExtractHighlights(restaurant, text, new[] { "Satay" });

        Assert.Contains("Laksa", hl);
        Assert.Contains("Roti", hl);
        Assert.Contains("too salty", hl);
        Assert.DoesNotContain("Satay", hl); // not mentioned in the text
    }
}
