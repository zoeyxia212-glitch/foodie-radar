namespace FoodieRadar.Api.Models;

public class Restaurant
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string EnglishName { get; set; } = "";

    /// <summary>Cuisine key, e.g. "sichuan". See <see cref="CuisineCatalog"/>.</summary>
    public string Cuisine { get; set; } = "";

    /// <summary>Broad region used for filtering, e.g. "North Shore". See <see cref="CuisineCatalog.Areas"/>.</summary>
    public string Area { get; set; } = "";

    /// <summary>Suburb shown to users, e.g. "Takapuna".</summary>
    public string Suburb { get; set; } = "";
    public string Address { get; set; } = "";
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? Website { get; set; }
    public string? Phone { get; set; }
    public string? OpeningHours { get; set; }

    /// <summary>0 = unknown, 1 = $, 2 = $$, 3 = $$$</summary>
    public int PriceLevel { get; set; }

    /// <summary>0 = not spicy / unknown, 5 = extremely spicy</summary>
    public int SpiceLevel { get; set; }

    // EF Core 8 stores primitive collections as JSON columns.
    public List<string> Tastes { get; set; } = new();
    public List<string> MustOrder { get; set; } = new();
    public List<string> Avoid { get; set; } = new();

    public string Summary { get; set; } = "";
    public string AiVerdict { get; set; } = "";

    /// <summary>OpenStreetMap element key, e.g. "node/123456". Null for non-OSM rows.</summary>
    public string? OsmKey { get; set; }

    /// <summary>True for the fictional demo rows created by DbSeeder.</summary>
    public bool IsDemo { get; set; }

    /// <summary>
    /// True when dishes, tastes, summary and verdict were written by hand.
    /// Curated rows are not overwritten by <c>RestaurantAggregator</c>.
    /// </summary>
    public bool IsCurated { get; set; }

    public List<Review> Reviews { get; set; } = new();
}
