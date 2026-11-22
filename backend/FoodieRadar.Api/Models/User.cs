namespace FoodieRadar.Api.Models;

public class User
{
    public int Id { get; set; }
    public string Email { get; set; } = "";
    public string DisplayName { get; set; } = "";

    /// <summary>Where the user is from. Used as ReviewerOrigin on their reviews.</summary>
    public string Origin { get; set; } = "";

    public string PasswordHash { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<Review> Reviews { get; set; } = new();
}

/// <summary>One "helpful" vote per user per review.</summary>
public class HelpfulVote
{
    public int UserId { get; set; }
    public int ReviewId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
