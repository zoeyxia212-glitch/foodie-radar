using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FoodieRadar.Api.Data;

/// <summary>
/// Used only by `dotnet ef` when creating migrations. Migrations target PostgreSQL;
/// the SQLite quick-start mode uses EnsureCreated and ignores them.
/// The database does not need to be running to add a migration.
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var cs = Environment.GetEnvironmentVariable("ConnectionStrings__Default")
                 ?? "Host=localhost;Port=5432;Database=foodie;Username=foodie;Password=foodie";

        var options = new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(cs).Options;
        return new AppDbContext(options);
    }
}
