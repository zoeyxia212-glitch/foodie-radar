using FoodieRadar.Api.Models;

namespace FoodieRadar.Api.Dtos;

public class RestaurantQuery
{
    public string[]? Cuisines { get; set; }
    public string[]? Tastes { get; set; }
    public string[]? Areas { get; set; }
    public int MinSpice { get; set; }

    /// <summary>Only return restaurants that have at least one native reviewer.</summary>
    public bool NativeOnly { get; set; }
    public string? Q { get; set; }

    /// <summary>"native" (default), "overall" or "reviews"</summary>
    public string? Sort { get; set; }

    /// <summary>Only return restaurants with at least one review.</summary>
    public bool HasReviews { get; set; }

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 24;

    /// <summary>User location, required for sort=distance.</summary>
    public double? Lat { get; set; }
    public double? Lon { get; set; }
}

public record RegisterRequest(string? Email, string? Password, string? DisplayName, string? Origin);
public record LoginRequest(string? Email, string? Password);
public record UserDto(int Id, string DisplayName, string Origin, string Email);
public record AuthResponse(string Token, UserDto User);

public record PagedResult<T>(List<T> Items, int Total, int Page, int PageSize);

public record RestaurantSummaryDto(
    int Id,
    string Name,
    string EnglishName,
    string Cuisine,
    string CuisineName,
    string Flag,
    string OriginLabel,
    string Area,
    string Suburb,
    int PriceLevel,
    int SpiceLevel,
    List<string> Tastes,
    List<string> MustOrder,
    List<string> Avoid,
    string Summary,
    double? OverallRating,
    double? NativeRating,
    int ReviewCount,
    int NativeReviewCount,
    Dictionary<string, int> SourceCounts,
    bool IsDemo,
    double? Latitude,
    double? Longitude,
    double? DistanceKm);

public record ReviewDto(
    int Id,
    string Source,
    string Author,
    string ReviewerOrigin,
    bool IsNative,
    int Rating,
    string Text,
    List<string> Highlights,
    List<string> RecommendedDishes,
    List<string> AvoidDishes,
    List<string> Tastes,
    int? SpiceRating,
    int? PriceLevel,
    int HelpfulCount,
    bool VotedByMe,
    bool IsMine,
    DateTime CreatedAt);

public record RestaurantDetailDto(
    RestaurantSummaryDto Restaurant,
    string AiVerdict,
    string Address,
    string? Website,
    string? Phone,
    string? OpeningHours,
    string? OsmUrl,
    List<ReviewDto> Reviews);

/// <summary>Author and origin come from the logged-in user's profile.</summary>
public record CreateReviewRequest(
    int Rating,
    string Text,
    List<string>? RecommendedDishes = null,
    List<string>? AvoidDishes = null,
    List<string>? Tastes = null,
    int? SpiceRating = null,
    int? PriceLevel = null);

public record MetaDto(
    IEnumerable<CuisineInfo> Cuisines,
    IEnumerable<string> Tastes,
    IEnumerable<string> Areas,
    IEnumerable<string> Origins);
