using System.Security.Claims;
using System.Text;
using FoodieRadar.Api.Models;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace FoodieRadar.Api.Services;

public class JwtOptions
{
    public string Issuer { get; set; } = "FoodieRadar";
    public string Audience { get; set; } = "FoodieRadar.Web";

    /// <summary>HMAC key, at least 32 characters. Keep it out of source control in production.</summary>
    public string Key { get; set; } = "";
    public int ExpiryDays { get; set; } = 7;

    public SymmetricSecurityKey SigningKey() => new(Encoding.UTF8.GetBytes(Key));
}

public class TokenService(JwtOptions options)
{
    public string Create(User user)
    {
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = options.Issuer,
            Audience = options.Audience,
            Expires = DateTime.UtcNow.AddDays(options.ExpiryDays),
            Claims = new Dictionary<string, object>
            {
                ["sub"] = user.Id.ToString(),
                ["name"] = user.DisplayName,
            },
            SigningCredentials = new SigningCredentials(options.SigningKey(), SecurityAlgorithms.HmacSha256)
        };
        return new JsonWebTokenHandler().CreateToken(descriptor);
    }
}

public static class ClaimsPrincipalExtensions
{
    /// <summary>Current user's id, or null when the request is anonymous.</summary>
    public static int? UserId(this ClaimsPrincipal principal) =>
        int.TryParse(principal.FindFirstValue("sub"), out var id) ? id : null;
}
