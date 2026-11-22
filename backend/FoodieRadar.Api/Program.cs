using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using FoodieRadar.Api.Data;
using FoodieRadar.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// "Sqlite" (default, zero setup) or "Postgres" (Docker / production).
var dbProvider = builder.Configuration.GetValue("Database:Provider", "Sqlite")!;
var usePostgres = dbProvider.Equals("Postgres", StringComparison.OrdinalIgnoreCase);
var connectionString = builder.Configuration.GetConnectionString("Default");

builder.Services.AddDbContext<AppDbContext>(o =>
{
    if (usePostgres)
        o.UseNpgsql(connectionString, npg => npg.EnableRetryOnFailure(5));
    else
        o.UseSqlite(connectionString);
});

builder.Services.AddHealthChecks();

// Behind the nginx container, take the client IP from X-Forwarded-For so per-IP
// rate limits work. Safe because the API port is not published outside Docker.
builder.Services.Configure<ForwardedHeadersOptions>(o =>
{
    o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    o.KnownNetworks.Clear();
    o.KnownProxies.Clear();
});

builder.Services.AddScoped<IReviewInsightService, KeywordInsightService>();

builder.Services.AddHttpClient<OsmImporter>(c =>
{
    c.Timeout = TimeSpan.FromMinutes(3);
    // Overpass asks clients to identify themselves.
    c.DefaultRequestHeaders.UserAgent.ParseAdd("FoodieRadar/0.1 (student project)");
});

// ---------- Auth (JWT) ----------
var jwt = builder.Configuration.GetSection("Jwt").Get<JwtOptions>() ?? new JwtOptions();
if (jwt.Key.Length < 32)
    throw new InvalidOperationException(
        "Jwt:Key must be at least 32 characters. Set it in appsettings.Development.json, " +
        "user-secrets or the Jwt__Key environment variable.");

builder.Services.AddSingleton(jwt);
builder.Services.AddSingleton<TokenService>();
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.MapInboundClaims = false; // keep "sub" / "name" as-is
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = jwt.SigningKey(),
            NameClaimType = "name",
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });
builder.Services.AddAuthorization();

// ---------- Rate limiting (per IP) ----------
builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    o.AddPolicy("auth", ctx => RateLimitPartition.GetFixedWindowLimiter(
        ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1) }));
    o.AddPolicy("writes", ctx => RateLimitPartition.GetFixedWindowLimiter(
        ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 30, Window = TimeSpan.FromMinutes(1) }));
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var corsOrigins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>()
                  ?? new[] { "http://localhost:5173" };
builder.Services.AddCors(o => o.AddPolicy("frontend", p =>
    p.WithOrigins(corsOrigins).AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

// Postgres: apply EF Core migrations. SQLite quick-start: create the schema directly.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    if (usePostgres)
    {
        if (!db.Database.GetMigrations().Any())
            throw new InvalidOperationException(
                "No EF Core migrations found. Run once in backend/FoodieRadar.Api: " +
                "dotnet ef migrations add InitialCreate");
        db.Database.Migrate();
    }
    else
    {
        db.Database.EnsureCreated();
    }

    // `dotnet run -- import-osm [--remove-demo]` imports real Auckland restaurants, then exits.
    if (args.Contains("import-osm"))
    {
        var importer = scope.ServiceProvider.GetRequiredService<OsmImporter>();
        var result = await importer.ImportAucklandAsync(removeDemo: args.Contains("--remove-demo"));
        Console.WriteLine(
            $"Fetched {result.Fetched}, created {result.Created}, updated {result.Updated}, " +
            $"skipped {result.Skipped}, demo removed {result.DemoRemoved}");
        return;
    }

    if (builder.Configuration.GetValue("SeedDemoData", true))
        DbSeeder.Seed(db);
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseForwardedHeaders();
app.UseCors("frontend");
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

public partial class Program { }
