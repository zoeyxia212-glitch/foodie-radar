using System.Net.Mail;
using FoodieRadar.Api.Data;
using FoodieRadar.Api.Dtos;
using FoodieRadar.Api.Models;
using FoodieRadar.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace FoodieRadar.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(AppDbContext db, TokenService tokens) : ControllerBase
{
    private static readonly PasswordHasher<User> Hasher = new();

    [HttpPost("register")]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest req)
    {
        var email = (req.Email ?? "").Trim().ToLowerInvariant();
        var errors = new Dictionary<string, string[]>();
        if (!IsEmail(email)) errors["email"] = new[] { "Please enter a valid email" };
        if (string.IsNullOrEmpty(req.Password) || req.Password.Length < 8)
            errors["password"] = new[] { "Password must be at least 8 characters" };
        if (string.IsNullOrWhiteSpace(req.DisplayName) || req.DisplayName.Trim().Length > 50)
            errors["displayName"] = new[] { "Display name is required (max 50 characters)" };
        if (!CuisineCatalog.Origins.Contains(req.Origin))
            errors["origin"] = new[] { "Please choose where you are from" };
        if (errors.Count > 0) return ValidationProblem(new ValidationProblemDetails(errors));

        if (await db.Users.AnyAsync(u => u.Email == email))
            return Conflict(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                ["email"] = new[] { "This email is already registered" }
            }));

        var user = new User { Email = email, DisplayName = req.DisplayName.Trim(), Origin = req.Origin };
        user.PasswordHash = Hasher.HashPassword(user, req.Password);
        db.Users.Add(user);
        await db.SaveChangesAsync();

        return new AuthResponse(tokens.Create(user), ToDto(user));
    }

    [HttpPost("login")]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest req)
    {
        var email = (req.Email ?? "").Trim().ToLowerInvariant();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email);

        // Same message for "no such user" and "wrong password" so emails can't be probed.
        if (user is null ||
            Hasher.VerifyHashedPassword(user, user.PasswordHash, req.Password ?? "") == PasswordVerificationResult.Failed)
        {
            return Unauthorized(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                ["login"] = new[] { "Incorrect email or password" }
            }));
        }

        return new AuthResponse(tokens.Create(user), ToDto(user));
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<UserDto>> Me()
    {
        var user = await db.Users.FindAsync(User.UserId());
        return user is null ? Unauthorized() : ToDto(user);
    }

    private static UserDto ToDto(User u) => new(u.Id, u.DisplayName, u.Origin, u.Email);

    private static bool IsEmail(string s) =>
        s.Length is > 3 and <= 254 && MailAddress.TryCreate(s, out var a) && a.Address == s;
}
