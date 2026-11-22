namespace FoodieRadar.Api.Models;

public enum ReviewSource
{
    Google,
    Xiaohongshu,
    Reddit,
    OnSite
}

public class Review
{
    public int Id { get; set; }
    public int RestaurantId { get; set; }
    public Restaurant? Restaurant { get; set; }

    /// <summary>Author account for on-site reviews; null for imported / seeded reviews.</summary>
    public int? UserId { get; set; }
    public User? User { get; set; }

    public ReviewSource Source { get; set; }
    public string Author { get; set; } = "";

    /// <summary>Where the reviewer is from, e.g. "Sichuan", "Japan", "New Zealand".</summary>
    public string ReviewerOrigin { get; set; } = "";

    public int Rating { get; set; }
    public string Text { get; set; } = "";

    /// <summary>Key phrases extracted from the text (dishes, taste notes).</summary>
    public List<string> Highlights { get; set; } = new();

    // Structured input from on-site reviewers. Aggregated into the restaurant profile.
    public List<string> RecommendedDishes { get; set; } = new();
    public List<string> AvoidDishes { get; set; } = new();
    public List<string> Tastes { get; set; } = new();

    /// <summary>0-5, null if the reviewer didn't say.</summary>
    public int? SpiceRating { get; set; }

    /// <summary>1-3 ($ to $$$), null if the reviewer didn't say.</summary>
    public int? PriceLevel { get; set; }

    public int HelpfulCount { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
