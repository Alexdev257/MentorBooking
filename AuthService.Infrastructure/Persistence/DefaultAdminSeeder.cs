using AuthService.Application.Interfaces.Helpers;
using AuthService.Application.Interfaces.Repositories;
using AuthService.Domain.Entities;
using AuthService.Domain.Enum;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AuthService.Infrastructure.Persistence;

public static class DefaultAdminSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
    {
        using var scope = serviceProvider.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IAuthUnitOfWork>();
        var bcrypt = scope.ServiceProvider.GetRequiredService<IBcryptHelper>();
        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DefaultAdminSeeder");

        var email = config["Admin:DefaultEmail"] ?? "admin@localhost";
        var password = config["Admin:DefaultPassword"] ?? "Admin@123";

        if (await unitOfWork.Users.AnyAsync(u => u.Role == (int)RoleNameEnum.Admin))
        {
            logger.LogInformation("Default admin already exists. Skipping seed.");
            return;
        }

        var hashedPassword = bcrypt.HashPassword(password);
        var adminUser = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            Password = hashedPassword,
            Fullname = "Administrator",
            Role = (int)RoleNameEnum.Admin,
            AvatarUrl = ""
        };

        await unitOfWork.Users.AddAsync(adminUser);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Default admin created. Email: {Email} (change password after first login if needed).", email);
    }
}
