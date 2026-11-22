using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace FoodieRadar.Api.Tests.Integration;

/// <summary>
/// Boots the real API in memory against a throwaway SQLite file seeded with the demo data.
/// </summary>
public class ApiFactory : WebApplicationFactory<Program>
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"foodie-test-{Guid.NewGuid():N}.db");

    public ApiFactory()
    {
        // Read by WebApplication.CreateBuilder, so set before the host starts.
        Environment.SetEnvironmentVariable("ConnectionStrings__Default", $"Data Source={_dbPath}");
        Environment.SetEnvironmentVariable("Database__Provider", "Sqlite");
        Environment.SetEnvironmentVariable("SeedDemoData", "true");
        Environment.SetEnvironmentVariable("Jwt__Key", "integration-test-key-0123456789-abcdefghij");
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        try { File.Delete(_dbPath); } catch (IOException) { /* best effort */ }
    }
}

public class ApiTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    private static async Task<JsonElement> Json(HttpResponseMessage res) =>
        await res.Content.ReadFromJsonAsync<JsonElement>();

    private async Task<string> RegisterAsync(string origin = "China")
    {
        var email = $"user{Guid.NewGuid():N}@example.com";
        var res = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            email, password = "password123", displayName = "Tester", origin
        });
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        return (await Json(res)).GetProperty("token").GetString()!;
    }

    private async Task<int> FirstRestaurantIdAsync(string cuisine)
    {
        var body = await Json(await _client.GetAsync($"/api/restaurants?cuisines={cuisine}"));
        return body.GetProperty("items")[0].GetProperty("id").GetInt32();
    }

    private HttpRequestMessage Authed(HttpMethod method, string url, string token, object? body = null)
    {
        var req = new HttpRequestMessage(method, url);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (body is not null) req.Content = JsonContent.Create(body);
        return req;
    }

    [Fact]
    public async Task Health_endpoint_is_up()
    {
        var res = await _client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }

    [Fact]
    public async Task Meta_returns_filter_options()
    {
        var body = await Json(await _client.GetAsync("/api/meta"));
        Assert.True(body.GetProperty("cuisines").GetArrayLength() > 0);
        Assert.Contains("Mala", body.GetProperty("tastes").EnumerateArray().Select(t => t.GetString()));
    }

    [Fact]
    public async Task List_returns_seeded_restaurants_with_paging()
    {
        var body = await Json(await _client.GetAsync("/api/restaurants?pageSize=3"));
        Assert.True(body.GetProperty("total").GetInt32() >= 8);
        Assert.Equal(3, body.GetProperty("items").GetArrayLength());
    }

    [Fact]
    public async Task Cuisine_filter_only_returns_that_cuisine()
    {
        var body = await Json(await _client.GetAsync("/api/restaurants?cuisines=sichuan"));
        var items = body.GetProperty("items").EnumerateArray().ToList();
        Assert.NotEmpty(items);
        Assert.All(items, i => Assert.Equal("sichuan", i.GetProperty("cuisine").GetString()));
    }

    [Fact]
    public async Task Unknown_restaurant_is_404()
    {
        var res = await _client.GetAsync("/api/restaurants/999999");
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    [Fact]
    public async Task Posting_a_review_requires_login()
    {
        var id = await FirstRestaurantIdAsync("thai");
        var res = await _client.PostAsJsonAsync($"/api/restaurants/{id}/reviews", new { rating = 5, text = "Great food here" });
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task Register_rejects_short_password()
    {
        var res = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            email = "short@example.com", password = "123", displayName = "X", origin = "China"
        });
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Logged_in_user_can_review_once_and_cannot_vote_on_own_review()
    {
        var token = await RegisterAsync(origin: "Vietnam");
        var id = await FirstRestaurantIdAsync("vietnam");

        // First review: created, uses the profile origin, counts as native for Vietnamese
        var first = await _client.SendAsync(Authed(HttpMethod.Post, $"/api/restaurants/{id}/reviews", token, new
        {
            rating = 5, text = "Rich broth and fresh herbs", recommendedDishes = new[] { "Banh mi" }, spiceRating = 1
        }));
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        var review = await Json(first);
        Assert.Equal("Vietnam", review.GetProperty("reviewerOrigin").GetString());
        Assert.True(review.GetProperty("isNative").GetBoolean());
        Assert.True(review.GetProperty("isMine").GetBoolean());

        // Second review on the same restaurant: rejected
        var second = await _client.SendAsync(Authed(HttpMethod.Post, $"/api/restaurants/{id}/reviews", token, new
        {
            rating = 4, text = "Trying to review twice"
        }));
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);

        // Voting on your own review: rejected
        var reviewId = review.GetProperty("id").GetInt32();
        var vote = await _client.SendAsync(Authed(HttpMethod.Post, $"/api/reviews/{reviewId}/helpful", token));
        Assert.Equal(HttpStatusCode.BadRequest, vote.StatusCode);
    }

    [Fact]
    public async Task Helpful_vote_counts_once_per_user()
    {
        var author = await RegisterAsync();
        var voter = await RegisterAsync();
        var id = await FirstRestaurantIdAsync("korea");

        var created = await _client.SendAsync(Authed(HttpMethod.Post, $"/api/restaurants/{id}/reviews", author, new
        {
            rating = 4, text = "Army stew was excellent"
        }));
        var reviewId = (await Json(created)).GetProperty("id").GetInt32();

        var v1 = await Json(await _client.SendAsync(Authed(HttpMethod.Post, $"/api/reviews/{reviewId}/helpful", voter)));
        var v2 = await Json(await _client.SendAsync(Authed(HttpMethod.Post, $"/api/reviews/{reviewId}/helpful", voter)));

        Assert.Equal(1, v1.GetProperty("helpfulCount").GetInt32());
        Assert.Equal(1, v2.GetProperty("helpfulCount").GetInt32());
    }

    [Fact]
    public async Task Invalid_review_input_is_rejected()
    {
        var token = await RegisterAsync();
        var id = await FirstRestaurantIdAsync("malay");

        var res = await _client.SendAsync(Authed(HttpMethod.Post, $"/api/restaurants/{id}/reviews", token, new
        {
            rating = 9, text = "ok", spiceRating = 7
        }));
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }
}
