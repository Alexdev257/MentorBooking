using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AuthService.Infrastructure.Persistence
{
    public sealed class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext(string[] args)
        {
            var cs =
                Environment.GetEnvironmentVariable("AUTH_DB")
                ?? "Host=localhost;Port=5432;Database=auth_db;Username=postgres;Password=12345";

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseNpgsql(cs)
                .Options;

            return new ApplicationDbContext(options);
        }
    }
}
