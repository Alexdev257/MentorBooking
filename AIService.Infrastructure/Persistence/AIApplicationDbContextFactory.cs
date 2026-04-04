using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AIService.Infrastructure.Persistence;

/// <summary>
/// Design-time factory cho EF Core migrations (dotnet ef migrations add).
/// Set env AI_DB nếu database khác localhost. Mặc định: Host=localhost;Database=ai_db;Username=postgres;Password=12345
/// </summary>
public sealed class AIApplicationDbContextFactory : IDesignTimeDbContextFactory<AIApplicationDbContext>
{
    public AIApplicationDbContext CreateDbContext(string[] args)
    {
        var cs = Environment.GetEnvironmentVariable("AI_DB")
            ?? "Host=localhost;Port=5432;Database=ai_db;Username=postgres;Password=12345";

        var options = new DbContextOptionsBuilder<AIApplicationDbContext>()
            .UseNpgsql(cs, npgsql => npgsql.CommandTimeout(1800))
            .Options;

        return new AIApplicationDbContext(options);
    }
}
