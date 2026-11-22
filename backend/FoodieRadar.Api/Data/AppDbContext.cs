using FoodieRadar.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace FoodieRadar.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Restaurant> Restaurants => Set<Restaurant>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<User> Users => Set<User>();
    public DbSet<HelpfulVote> HelpfulVotes => Set<HelpfulVote>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Restaurant>(e =>
        {
            e.Property(r => r.Name).HasMaxLength(100);
            e.Property(r => r.Cuisine).HasMaxLength(30);
            e.HasIndex(r => r.Cuisine);
            e.HasIndex(r => r.Area);
            e.HasIndex(r => r.OsmKey).IsUnique();
            e.HasMany(r => r.Reviews)
                .WithOne(v => v.Restaurant)
                .HasForeignKey(v => v.RestaurantId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Review>(e =>
        {
            e.Property(v => v.Source).HasConversion<string>().HasMaxLength(20);
            e.Property(v => v.Author).HasMaxLength(50);
            e.Property(v => v.ReviewerOrigin).HasMaxLength(30);
            e.Property(v => v.Text).HasMaxLength(2000);
            // One review per user per restaurant (NULL user ids are not compared, so imports are fine).
            e.HasIndex(v => new { v.RestaurantId, v.UserId }).IsUnique();
            e.HasOne(v => v.User)
                .WithMany(u => u.Reviews)
                .HasForeignKey(v => v.UserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<User>(e =>
        {
            e.Property(u => u.Email).HasMaxLength(254);
            e.Property(u => u.DisplayName).HasMaxLength(50);
            e.Property(u => u.Origin).HasMaxLength(30);
            e.HasIndex(u => u.Email).IsUnique();
        });

        modelBuilder.Entity<HelpfulVote>(e =>
        {
            e.HasKey(v => new { v.UserId, v.ReviewId });
            e.HasOne<User>().WithMany().HasForeignKey(v => v.UserId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne<Review>().WithMany().HasForeignKey(v => v.ReviewId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}
