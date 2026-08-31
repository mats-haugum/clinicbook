using ClinicAppointmentBookingSystem.Data;
using ClinicAppointmentBookingSystem.Models.Entities;
using ClinicAppointmentBookingSystem.Services.Admin;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ClinicAppointmentBookingSystem.IntegrationTests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    // Single source of truth for the test connection string.
    private const string TestConnectionString =
        "Server=localhost,1433;Database=ClinicBookingDB_Test;User Id=sa;Password=Admin@123;TrustServerCertificate=true;";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<ClinicBookingDbContext>));

            if (descriptor != null)
                services.Remove(descriptor);

            services.AddDbContext<ClinicBookingDbContext>(options =>
                options.UseSqlServer(TestConnectionString));
        });

        // Program.cs reads its rate limits from the RateLimiting section, so
        // this override (added AFTER appsettings.json - later config
        // providers win) raises them far past anything a functional test
        // could hit. Ordinary CRUD/auth tests aren't testing rate limiting
        // and shouldn't have to dodge production-tuned limits just because
        // they call an endpoint like /auth/register once per test. Tests
        // that specifically want the real limits enforced (see
        // RateLimiting/AuthRateLimitingTests.cs) use their own factory
        // subclass that layers the real values back on top of this.
        //
        // DefaultConnection is overridden here as well as on the DbContext
        // above, because the /health check reads the connection string from
        // configuration rather than from the DbContext. Without this it would
        // probe the real ClinicBookingDB - a database that exists on a
        // developer machine that has run `dotnet ef database update`, but
        // never on a clean CI runner, making the health test pass or fail
        // depending on local state rather than on the code.
        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["RateLimiting:ApiPermitLimit"] = "1000000",
                ["RateLimiting:AuthPermitLimit"] = "1000000",
                ["ConnectionStrings:DefaultConnection"] = TestConnectionString,
            }));

        builder.UseEnvironment("Development");
    }

    // CreateHost is called once when the first client is created.
    // We ensure the test DB exists and has the current schema BEFORE base.CreateHost(builder)
    // starts the app — otherwise Program.cs's AdminSeeder crashes on a non-existent database.
    protected override IHost CreateHost(IHostBuilder builder)
    {
        EnsureDatabaseReady();
        return base.CreateHost(builder);
    }

    // Called by each test class's InitializeAsync to wipe and recreate the DB between test runs.
    public void ResetDatabase() => EnsureDatabaseReady();

    private static void EnsureDatabaseReady()
    {
        var options = new DbContextOptionsBuilder<ClinicBookingDbContext>()
            .UseSqlServer(TestConnectionString)
            .Options;

        using var db = new ClinicBookingDbContext(options);
        db.Database.EnsureDeleted();
        db.Database.Migrate();

        // AdminSeeder only runs at app startup, so after each wipe we re-seed manually.
        // These credentials match the AdminSeed section in appsettings.json.
        var salt = AdminAuthService.GenerateSalt();
        db.Admins.Add(new Admin
        {
            Email        = "admin@clinicbook.com",
            FirstName    = "Admin",
            LastName     = "User",
            PasswordSalt = salt,
            PasswordHash = AdminAuthService.HashPassword("Admin@123", salt),
        });
        db.SaveChanges();
    }
}
