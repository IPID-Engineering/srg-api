using Microsoft.EntityFrameworkCore;
using SRG.Application.Auth;
using SRG.Domain.Entities;
using SRG.Domain.Enums;
using SRG.Infrastructure.Persistence;

namespace SRG.Api.Extensions;

public static class SeedAdminExtensions
{
    public static async Task SeedAdminUserAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var passwordService = scope.ServiceProvider.GetRequiredService<IPasswordService>();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

        await dbContext.Database.MigrateAsync();

        if (await dbContext.Users.AnyAsync(user => user.Role == UserRole.Admin))
        {
            return;
        }

        var email = configuration["SeedAdmin:Email"] ?? "admin@srg.local";
        var password = configuration["SeedAdmin:Password"] ?? "Admin123!";
        var firstName = configuration["SeedAdmin:FirstName"] ?? "System";
        var lastName = configuration["SeedAdmin:LastName"] ?? "Administrator";

        dbContext.Users.Add(new User
        {
            Email = email.Trim().ToLowerInvariant(),
            PasswordHash = passwordService.HashPassword(password),
            FirstName = firstName,
            LastName = lastName,
            Role = UserRole.Admin,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
        });

        await dbContext.SaveChangesAsync();
    }
}
