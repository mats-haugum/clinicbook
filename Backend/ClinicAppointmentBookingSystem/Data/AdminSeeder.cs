using ClinicAppointmentBookingSystem.Models.Entities;
using ClinicAppointmentBookingSystem.Services.Admin;

namespace ClinicAppointmentBookingSystem.Data;

public static class AdminSeeder
{
    // Runs once on startup. If no admin row exists, creates one from the AdminSeed
    // section in appsettings.json. This means credentials are never committed to source
    // control as a migration — change them in appsettings without touching any code.
    public static async Task SeedAsync(ClinicBookingDbContext db, IConfiguration config)
    {
        if (db.Admins.Any())
            return;

        var seed = config.GetSection("AdminSeed");
        var email     = seed["Email"]     ?? throw new InvalidOperationException("AdminSeed:Email is not configured.");
        var password  = seed["Password"]  ?? throw new InvalidOperationException("AdminSeed:Password is not configured.");
        var firstName = seed["FirstName"] ?? "Admin";
        var lastName  = seed["LastName"]  ?? "User";

        var salt = AdminAuthService.GenerateSalt();

        db.Admins.Add(new Admin
        {
            Email        = email,
            FirstName    = firstName,
            LastName     = lastName,
            PasswordSalt = salt,
            PasswordHash = AdminAuthService.HashPassword(password, salt),
        });

        await db.SaveChangesAsync();
    }
}
