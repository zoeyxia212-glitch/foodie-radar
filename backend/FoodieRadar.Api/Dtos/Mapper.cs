using FoodieRadar.Api.Models;

namespace FoodieRadar.Api.Dtos;

public static class Mapper
{
    /// <summary>Requires r.Reviews to be loaded. Pass the user's location to get DistanceKm.</summary>
    public static RestaurantSummaryDto ToSummary(Restaurant r, double? fromLat = null, double? fromLon = null)
    {
        var cuisine = CuisineCatalog.Find(r.Cuisine);
        var native = r.Reviews.Where(v => CuisineCatalog.IsNative(r.Cuisine, v.ReviewerOrigin)).ToList();

        double? overall = r.Reviews.Count == 0 ? null : Math.Round(r.Reviews.Average(v => v.Rating), 1);
        double? nativeAvg = native.Count == 0 ? null : Math.Round(native.Average(v => v.Rating), 1);

        var sourceCounts = r.Reviews
            .GroupBy(v => v.Source.ToString())
            .ToDictionary(g => g.Key, g => g.Count());

        double? distance = fromLat is not null && fromLon is not null && r.Latitude is not null && r.Longitude is not null
            ? Math.Round(Geo.DistanceKm(fromLat.Value, fromLon.Value, r.Latitude.Value, r.Longitude.Value), 1)
            : null;

        return new RestaurantSummaryDto(
            r.Id,
            r.Name,
            r.EnglishName,
            r.Cuisine,
            cuisine?.Name ?? r.Cuisine,
            cuisine?.Flag ?? "",
            cuisine?.OriginLabel ?? "",
            r.Area,
            r.Suburb,
            r.PriceLevel,
            r.SpiceLevel,
            r.Tastes,
            r.MustOrder,
            r.Avoid,
            r.Summary,
            overall,
            nativeAvg,
            r.Reviews.Count,
            native.Count,
            sourceCounts,
            r.IsDemo,
            r.Latitude,
            r.Longitude,
            distance);
    }

    public static RestaurantDetailDto ToDetail(Restaurant r, int? userId, ISet<int> votedReviewIds) => new(
        ToSummary(r),
        r.AiVerdict,
        r.Address,
        r.Website,
        r.Phone,
        r.OpeningHours,
        r.OsmKey is null ? null : $"https://www.openstreetmap.org/{r.OsmKey}",
        r.Reviews
            .OrderByDescending(v => v.CreatedAt)
            .Select(v => ToDto(v, r.Cuisine, userId, votedReviewIds.Contains(v.Id)))
            .ToList());

    public static ReviewDto ToDto(Review v, string cuisineKey, int? userId = null, bool votedByMe = false) => new(
        v.Id,
        v.Source.ToString(),
        v.Author,
        v.ReviewerOrigin,
        CuisineCatalog.IsNative(cuisineKey, v.ReviewerOrigin),
        v.Rating,
        v.Text,
        v.Highlights,
        v.RecommendedDishes,
        v.AvoidDishes,
        v.Tastes,
        v.SpiceRating,
        v.PriceLevel,
        v.HelpfulCount,
        votedByMe,
        userId is not null && v.UserId == userId,
        v.CreatedAt);
}

public static class Geo
{
    /// <summary>Great-circle distance (haversine) in kilometres.</summary>
    public static double DistanceKm(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371;
        double Rad(double d) => d * Math.PI / 180;
        var dLat = Rad(lat2 - lat1);
        var dLon = Rad(lon2 - lon1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(Rad(lat1)) * Math.Cos(Rad(lat2)) * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        return 2 * R * Math.Asin(Math.Sqrt(a));
    }
}
