using Microsoft.EntityFrameworkCore;
using API.Common.Cli;
using API.Data;
using API.Features.Auth.Models;
using API.Features.Projects.Models;

namespace API.Cli.Commands;

[CommandGroup("seed")]
[Command("admin", "Seed a default admin user for development")]
public class SeedAdminCommand(AppDbContext db) : CliCommand
{
    public override async Task ExecuteAsync(string[] args, CancellationToken ct)
    {
        if (await db.Users.AnyAsync(ct))
        {
            Console.Error.WriteLine("Database already has users. Skipping seed.");
            return;
        }

        var hash = BCrypt.Net.BCrypt.HashPassword("Admin1234!");
        var user = new User
        {
            Email = "admin@taskr.local",
            FirstName = "Admin",
            LastName = "User",
            PasswordHash = hash,
            IsActive = true
        };
        db.Users.Add(user);
        await db.SaveChangesAsync(ct);

        var project = new Project
        {
            Name = "Sample Project",
            Description = "Auto-seeded project",
            OwnerId = user.Id
        };
        db.Projects.Add(project);
        await db.SaveChangesAsync(ct);

        Console.WriteLine($"Seeded admin user ({user.Email}) and sample project.");
    }
}
