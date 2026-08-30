using BirthdayGifts.Api.Data;
using BirthdayGifts.Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BirthdayGifts.Api.Services;

public sealed class AdminSeeder(AppDbContext db, IPasswordHasher<AdminUser> passwordHasher, IConfiguration configuration, ILogger<AdminSeeder> logger)
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var username = configuration["ADMIN_USERNAME"];
        var password = configuration["ADMIN_PASSWORD"];

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            logger.LogWarning("ADMIN_USERNAME or ADMIN_PASSWORD is not configured. Initial admin was not created.");
            return;
        }

        var exists = await db.AdminUsers.AnyAsync(a => a.Username == username, cancellationToken);
        if (exists)
        {
            var existingUser = await db.AdminUsers.SingleAsync(a => a.Username == username, cancellationToken);
            existingUser.PasswordHash = passwordHasher.HashPassword(existingUser, password);
            await db.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Admin user password hash was refreshed from ADMIN_PASSWORD.");
            return;
        }

        var user = new AdminUser
        {
            Username = username,
            PasswordHash = string.Empty,
            CreatedAt = DateTime.UtcNow
        };
        user.PasswordHash = passwordHasher.HashPassword(user, password);
        db.AdminUsers.Add(user);
        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Initial admin user was created.");
    }
}
