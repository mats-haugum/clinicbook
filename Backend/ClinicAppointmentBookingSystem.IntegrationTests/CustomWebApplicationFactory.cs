using ClinicAppointmentBookingSystem.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ClinicAppointmentBookingSystem.IntegrationTests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<ClinicBookingDbContext>));

            if (descriptor != null)
                services.Remove(descriptor);

            services.AddDbContext<ClinicBookingDbContext>(options =>
                options.UseSqlServer(
                    "Server=localhost,1433;Database=ClinicBookingDB_Test;User Id=sa;Password=Admin@123;TrustServerCertificate=true;"));
        });

        builder.UseEnvironment("Development");
    }

    public void ResetDatabase()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ClinicBookingDbContext>();
        db.Database.EnsureDeleted();
        db.Database.Migrate();
    }
}
