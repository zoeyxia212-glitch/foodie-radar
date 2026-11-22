using FoodieRadar.Api.Data;
using FoodieRadar.Api.Models;
using FoodieRadar.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace FoodieRadar.Api.Controllers;

[ApiController]
[Route("api/reviews")]
[Authorize]
public class ReviewsController(AppDbContext db) : ControllerBase
{
    /// <summary>Marks a review as helpful. One vote per user; you can't vote on your own review.</summary>
    [HttpPost("{id:int}/helpful")]
    [EnableRateLimiting("writes")]
    public async Task<ActionResult<object>> Helpful(int id)
    {
        var userId = User.UserId();
        if (userId is null) return Unauthorized();

        var review = await db.Reviews.FirstOrDefaultAsync(v => v.Id == id);
        if (review is null) return NotFound();
        if (review.UserId == userId) return BadRequest(new { message = "You can't vote on your own review" });

        var already = await db.HelpfulVotes.AnyAsync(h => h.UserId == userId && h.ReviewId == id);
        if (!already)
        {
            db.HelpfulVotes.Add(new HelpfulVote { UserId = userId.Value, ReviewId = id });
            review.HelpfulCount++;
            await db.SaveChangesAsync();
        }

        return new { helpfulCount = review.HelpfulCount };
    }
}
