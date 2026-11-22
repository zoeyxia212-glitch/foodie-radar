using FoodieRadar.Api.Data;
using FoodieRadar.Api.Dtos;
using FoodieRadar.Api.Models;
using FoodieRadar.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace FoodieRadar.Api.Controllers;

[ApiController]
[Route("api/restaurants")]
public class RestaurantsController(AppDbContext db, IReviewInsightService insights) : ControllerBase
{
    private const int MaxPageSize = 60;

    // GET /api/restaurants?cuisines=sichuan&tastes=Mala&minSpice=3&nativeOnly=true&sort=native&q=fish&page=1
    [HttpGet]
    public async Task<ActionResult<PagedResult<RestaurantSummaryDto>>> List([FromQuery] RestaurantQuery query)
    {
        IQueryable<Restaurant> q = db.Restaurants.AsNoTracking().Include(r => r.Reviews);

        if (query.Cuisines is { Length: > 0 } cuisines)
            q = q.Where(r => cuisines.Contains(r.Cuisine));
        if (query.Areas is { Length: > 0 } areas)
            q = q.Where(r => areas.Contains(r.Area));
        if (query.MinSpice > 0)
            q = q.Where(r => r.SpiceLevel >= query.MinSpice);
        if (query.HasReviews)
            q = q.Where(r => r.Reviews.Any());

        // Remaining filters and scoring run in memory. Fine for a few thousand rows;
        // move ratings into stored columns (or a search index) once data grows.
        IEnumerable<Restaurant> list = await q.ToListAsync();

        if (query.Tastes is { Length: > 0 } tastes)
            list = list.Where(r => tastes.All(t => r.Tastes.Contains(t)));

        if (!string.IsNullOrWhiteSpace(query.Q))
        {
            var term = query.Q.Trim();
            list = list.Where(r =>
                r.Name.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                r.EnglishName.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                r.Suburb.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                r.Summary.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                r.MustOrder.Any(d => d.Contains(term, StringComparison.OrdinalIgnoreCase)));
        }

        var dtos = list.Select(r => Mapper.ToSummary(r, query.Lat, query.Lon));

        if (query.NativeOnly)
            dtos = dtos.Where(d => d.NativeReviewCount > 0);

        dtos = query.Sort switch
        {
            "distance" when query.Lat is not null && query.Lon is not null =>
                dtos.OrderBy(d => d.DistanceKm ?? double.MaxValue),
            "overall" => dtos.OrderByDescending(d => d.OverallRating ?? 0).ThenByDescending(d => d.ReviewCount),
            "reviews" => dtos.OrderByDescending(d => d.ReviewCount),
            _ => dtos.OrderByDescending(d => d.NativeRating ?? 0)
                     .ThenByDescending(d => d.OverallRating ?? 0)
                     .ThenByDescending(d => d.ReviewCount)
        };

        var all = dtos.ToList();
        var page = Math.Max(1, query.Page);
        var size = Math.Clamp(query.PageSize, 1, MaxPageSize);
        var items = all.Skip((page - 1) * size).Take(size).ToList();

        return new PagedResult<RestaurantSummaryDto>(items, all.Count, page, size);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<RestaurantDetailDto>> Get(int id)
    {
        var r = await db.Restaurants.AsNoTracking()
            .Include(x => x.Reviews)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (r is null) return NotFound();

        // Anonymous requests work too; a valid token just adds "voted by me" / "is mine" flags.
        var userId = User.UserId();
        var reviewIds = r.Reviews.Select(v => v.Id).ToList();
        var voted = userId is null
            ? new HashSet<int>()
            : (await db.HelpfulVotes
                .Where(h => h.UserId == userId && reviewIds.Contains(h.ReviewId))
                .Select(h => h.ReviewId)
                .ToListAsync()).ToHashSet();

        return Mapper.ToDetail(r, userId, voted);
    }

    [HttpPost("{id:int}/reviews")]
    [Authorize]
    [EnableRateLimiting("writes")]
    public async Task<ActionResult<ReviewDto>> AddReview(int id, CreateReviewRequest req)
    {
        var errors = Validate(req);
        if (errors.Count > 0) return ValidationProblem(new ValidationProblemDetails(errors));

        var user = await db.Users.FindAsync(User.UserId());
        if (user is null) return Unauthorized();

        var r = await db.Restaurants.Include(x => x.Reviews).FirstOrDefaultAsync(x => x.Id == id);
        if (r is null) return NotFound();

        if (r.Reviews.Any(v => v.UserId == user.Id))
            return Conflict(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                ["review"] = new[] { "You have already reviewed this restaurant" }
            }));

        var text = req.Text.Trim();
        var recommended = CleanDishes(req.RecommendedDishes);
        var avoid = CleanDishes(req.AvoidDishes);

        var review = new Review
        {
            Source = ReviewSource.OnSite,
            UserId = user.Id,
            Author = user.DisplayName,
            ReviewerOrigin = user.Origin,
            Rating = req.Rating,
            Text = text,
            RecommendedDishes = recommended,
            AvoidDishes = avoid,
            Tastes = (req.Tastes ?? new()).Distinct().ToList(),
            SpiceRating = req.SpiceRating,
            PriceLevel = req.PriceLevel,
            Highlights = insights.ExtractHighlights(r, text, recommended.Concat(avoid)),
            CreatedAt = DateTime.UtcNow
        };

        r.Reviews.Add(review);
        RestaurantAggregator.Recompute(r);
        await db.SaveChangesAsync();

        return CreatedAtAction(nameof(Get), new { id }, Mapper.ToDto(review, r.Cuisine, user.Id));
    }

    private static List<string> CleanDishes(List<string>? dishes) =>
        (dishes ?? new())
            .Select(d => d.Trim())
            .Where(d => d.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

    private static Dictionary<string, string[]> Validate(CreateReviewRequest req)
    {
        var e = new Dictionary<string, string[]>();
        if (req.Rating is < 1 or > 5)
            e["rating"] = new[] { "Rating must be between 1 and 5" };
        if (string.IsNullOrWhiteSpace(req.Text) || req.Text.Trim().Length < 5 || req.Text.Length > 2000)
            e["text"] = new[] { "Review must be 5 to 2000 characters" };
        if (TooManyOrTooLong(req.RecommendedDishes) || TooManyOrTooLong(req.AvoidDishes))
            e["dishes"] = new[] { "Up to 5 dishes each to order / skip, max 30 characters per dish" };
        if (req.Tastes is not null && req.Tastes.Any(t => !CuisineCatalog.Tastes.Contains(t)))
            e["tastes"] = new[] { "Invalid taste tag" };
        if (req.SpiceRating is < 0 or > 5)
            e["spiceRating"] = new[] { "Spice rating must be between 0 and 5" };
        if (req.PriceLevel is < 1 or > 3)
            e["priceLevel"] = new[] { "Price level must be between 1 and 3" };
        return e;
    }

    private static bool TooManyOrTooLong(List<string>? dishes) =>
        dishes is not null && (dishes.Count > 5 || dishes.Any(d => d is null || d.Trim().Length > 30));
}
