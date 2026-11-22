using FoodieRadar.Api.Models;

namespace FoodieRadar.Api.Data;

/// <summary>
/// Seeds sample data on first run. All restaurant names and reviews are fictional.
/// Each highlight must appear verbatim in its review text so the frontend can mark it.
/// </summary>
public static class DbSeeder
{
    public static void Seed(AppDbContext db)
    {
        if (db.Restaurants.Any()) return;

        var now = DateTime.UtcNow;
        int day = 0;
        Review R(ReviewSource s, string author, string origin, int rating, string text, params string[] hl) => new()
        {
            Source = s, Author = author, ReviewerOrigin = origin, Rating = rating,
            Text = text, Highlights = hl.ToList(), CreatedAt = now.AddDays(-(++day))
        };

        const ReviewSource G = ReviewSource.Google, X = ReviewSource.Xiaohongshu,
                           Rd = ReviewSource.Reddit, O = ReviewSource.OnSite;

        var demo = new List<Restaurant>
        {
            new Restaurant
            {
                Name = "Shu Taste Kitchen", EnglishName = "Shu Taste Kitchen", Cuisine = "sichuan", Area = "North Shore",
                PriceLevel = 2, SpiceLevel = 4, Tastes = { "Mala", "Bold", "Wok hei" },
                MustOrder = { "Boiled fish in chilli oil", "Mouth-watering chicken", "Dry-fried green beans" },
                Avoid = { "Kung pao chicken" },
                Summary = "Properly numbing Sichuan pepper. Sichuan reviewers love it; some locals find it too salty and oily.",
                AiVerdict = "Go for real mala. Skip the kung pao chicken, which is a sweet, localised version. Expect a queue on Friday nights.",
                Reviews =
                {
                    R(X, "Chengdu local in AKL", "Sichuan", 5, "Finally a North Shore place where the Sichuan pepper is genuinely numbing! The boiled fish gets fresh hot oil poured over at the table.", "genuinely numbing", "boiled fish"),
                    R(G, "Mike T.", "New Zealand", 3, "Way too spicy and salty for me. Service was quick though.", "too spicy and salty"),
                    R(O, "Zoey", "China", 4, "Mouth-watering chicken is a must. The kung pao chicken is on the sweet side, clearly adapted for locals.", "Mouth-watering chicken", "kung pao chicken is on the sweet side"),
                    R(Rd, "u/aklfoodie", "China", 5, "Legit Sichuan. Ask for the Chinese menu, it has more dishes.", "Chinese menu"),
                }
            },
            new Restaurant
            {
                Name = "Harbour Cha Chaan Teng", EnglishName = "Harbour Cha Chaan Teng", Cuisine = "canton", Area = "CBD",
                PriceLevel = 1, SpiceLevel = 0, Tastes = { "Fresh", "Wok hei", "Light" },
                MustOrder = { "Dry-fried beef ho fun", "HK milk tea", "Pineapple bun with butter" },
                Avoid = { "Curry fish balls" },
                Summary = "Hong Kong reviewers rate the milk tea and beef ho fun highly. Low scores are mostly about service, not food.",
                AiVerdict = "The food is better than the overall score suggests. Most complaints are about waiting and service.",
                Reviews =
                {
                    R(X, "HK boy", "Hong Kong", 5, "The beef ho fun has serious wok hei and the milk tea is silky. Feels like home.", "serious wok hei"),
                    R(G, "Sarah L.", "UK", 2, "Staff were rude and we waited 20 mins for a table.", "waited 20 mins"),
                    R(O, "May", "Guangdong", 4, "The pineapple bun is spot on. The fish balls are just OK.", "pineapple bun"),
                }
            },
            new Restaurant
            {
                Name = "Kaze Robata", EnglishName = "Kaze Robata", Cuisine = "japan", Area = "Newmarket",
                PriceLevel = 3, SpiceLevel = 0, Tastes = { "Fresh", "Light" },
                MustOrder = { "Seared salmon belly", "Chicken yakitori" }, Avoid = { "Ramen" },
                Summary = "Highly rated overall, but Japanese reviewers find the sauces sweeter than in Japan.",
                AiVerdict = "Great for a date or a business dinner. Japanese reviewers score it lower if you want an authentic izakaya.",
                Reviews =
                {
                    R(G, "Yuki S.", "Japan", 3, "The sashimi is fresh, but the tare sauce is a bit too sweet for me.", "a bit too sweet"),
                    R(G, "James W.", "New Zealand", 5, "Best date night spot! The salmon belly is incredible.", "salmon belly"),
                    R(X, "Tokyo stomach", "China", 4, "Lovely interior. Skip the ramen though, the broth is too thin.", "Skip the ramen"),
                }
            },
            new Restaurant
            {
                Name = "Seoul Pocha", EnglishName = "Seoul Pocha", Cuisine = "korea", Area = "CBD",
                PriceLevel = 2, SpiceLevel = 3, Tastes = { "Sweet", "Bold", "Sour" },
                MustOrder = { "Army stew", "Soy garlic fried chicken", "House kimchi" }, Avoid = { "Stone pot bibimbap" },
                Summary = "Popular with Korean students. Korean reviewers rate the army stew and fried chicken highly; the kimchi is made in-house.",
                AiVerdict = "Koreans eat here too, which is a good sign. Best for groups of four or more.",
                Reviews =
                {
                    R(G, "Minji K.", "Korea", 5, "The kimchi tastes homemade! The army stew is really good.", "kimchi tastes homemade"),
                    R(X, "K-drama fan", "China", 4, "The soy garlic chicken beats the sweet chilli one. Bibimbap was average.", "soy garlic chicken"),
                }
            },
            new Restaurant
            {
                Name = "Pho Me", EnglishName = "Mum's Pho", Cuisine = "vietnam", Area = "Dominion Rd",
                PriceLevel = 1, SpiceLevel = 1, Tastes = { "Fresh", "Rich broth", "Light" },
                MustOrder = { "Rare and well-done beef pho", "Banh mi" },
                Summary = "Vietnamese reviewers give it top marks for a deep beef bone broth. Great value.",
                AiVerdict = "The most authentic pho in the area. Get there before 11:30 on weekends.",
                Reviews =
                {
                    R(G, "Linh N.", "Vietnam", 5, "Just like pho in Hanoi. The broth is sweet from the bones.", "Just like pho in Hanoi"),
                    R(Rd, "u/pho_hunter", "New Zealand", 5, "Best pho in Auckland, $16 for a huge bowl.", "$16 for a huge bowl"),
                    R(O, "Lin", "China", 4, "The broth is really rich. Seating is a bit cramped.", "broth is really rich"),
                }
            },
            new Restaurant
            {
                Name = "Bangkok Street", EnglishName = "Bangkok Street", Cuisine = "thai", Area = "North Shore",
                PriceLevel = 2, SpiceLevel = 3, Tastes = { "Sour", "Sweet", "Mala" },
                MustOrder = { "Pad thai", "Green curry" }, Avoid = { "Tom yum" },
                Summary = "Locals love it; Thai reviewers say it is too sweet and not spicy enough.",
                AiVerdict = "Friendly to local tastes, but you may be disappointed if you want authentic Thai.",
                Reviews =
                {
                    R(G, "Ploy P.", "Thailand", 3, "Too sweet, tom yum not sour enough. Not like Bangkok.", "Too sweet"),
                    R(G, "Emma R.", "New Zealand", 5, "Love the green curry! Perfect spice level.", "green curry"),
                }
            },
            new Restaurant
            {
                Name = "Kopitiam 88", EnglishName = "Kopitiam 88", Cuisine = "malay", Area = "Botany",
                PriceLevel = 1, SpiceLevel = 2, Tastes = { "Rich broth", "Bold", "Fresh" },
                MustOrder = { "Curry laksa", "Nasi lemak", "Hainanese chicken rice" }, Avoid = { "Roti" },
                Summary = "Malaysian reviewers rate the laksa and nasi lemak; the sambal is made in-house.",
                AiVerdict = "Great value in the east. The laksa gets the most praise.",
                Reviews =
                {
                    R(X, "Penang girl", "Malaysia", 5, "The sambal is homemade and the laksa broth is properly rich!", "sambal is homemade"),
                    R(G, "Daniel O.", "New Zealand", 4, "Great laksa. Roti was a bit oily.", "Roti was a bit oily"),
                }
            },
            new Restaurant
            {
                Name = "Old Chengdu Skewers", EnglishName = "Old Chengdu Skewers", Cuisine = "sichuan", Area = "Dominion Rd",
                PriceLevel = 2, SpiceLevel = 5, Tastes = { "Mala", "Bold", "Rich broth" },
                MustOrder = { "Beef tallow hotpot base", "Chicken gizzards", "Green Sichuan pepper chicken" },
                Summary = "The overall score is low because many locals can't handle the heat. Chinese reviewers rate it very highly.",
                AiVerdict = "Badly underrated overall. With native reviewers only, it scores much higher. Not for the spice-shy.",
                Reviews =
                {
                    R(X, "Chongqing hotpot fan", "Chongqing", 5, "The beef tallow base is properly fried in-house, not from a packet.", "beef tallow base"),
                    R(G, "Tom H.", "Australia", 2, "My mouth was on fire for an hour. Not for me.", "on fire"),
                    R(O, "Zoey", "China", 5, "The green Sichuan pepper chicken was a great surprise. Will be back!", "green Sichuan pepper chicken"),
                }
            }
        };

        // Demo rows use a suburb name in Area; convert it to a region.
        var regionOf = new Dictionary<string, string>
        {
            ["North Shore"] = "North Shore",
            ["CBD"] = "Central Auckland",
            ["Dominion Rd"] = "Central Auckland",
            ["Newmarket"] = "Central Auckland",
            ["Botany"] = "East Auckland",
        };
        foreach (var r in demo)
        {
            r.Suburb = r.Area;
            r.Area = regionOf.GetValueOrDefault(r.Area, "Central Auckland");
            r.IsDemo = true;
            r.IsCurated = true;
        }

        db.Restaurants.AddRange(demo);
        db.SaveChanges();
    }
}
