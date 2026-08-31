using System.Net.Http.Json;
using ClinicAppointmentBookingSystem.Data;
using ClinicAppointmentBookingSystem.Models.DTOs.Appointments;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ClinicAppointmentBookingSystem.IntegrationTests.Demo;

// DemoResetService isn't reachable over HTTP - the --reset-demo CLI mode in
// Program.cs runs before Kestrel starts and returns immediately. These tests
// call it directly against a scoped ClinicBookingDbContext instead, the same
// pattern AuthControllerTests.ExpireRefreshTokenInDbAsync uses to reach past
// the API when a test needs to manipulate rows the API itself can't reach.
public class DemoResetServiceTests(CustomWebApplicationFactory factory)
    : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly HttpClient _client = factory.CreateClient();

    public Task InitializeAsync()
    {
        factory.ResetDatabase();
        return Task.CompletedTask;
    }

    public Task DisposeAsync() => Task.CompletedTask;

    // -------------------------------------------------------------------------
    // IsDirtyAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task IsDirtyAsync_OnFreshlySeededDatabase_ReturnsFalse()
    {
        // ResetDatabase() (called above in InitializeAsync) runs EnsureDeleted +
        // Migrate, which reapplies the same HasData seed baked into the
        // migrations. If this is ever false when it shouldn't be, it means
        // DemoResetService's hardcoded seed constants have drifted from
        // ClinicBookingDbContext.OnModelCreating's HasData - the two are meant
        // to describe the same seed and must be kept in sync by hand.
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ClinicBookingDbContext>();

        var isDirty = await DemoResetService.IsDirtyAsync(db);

        isDirty.Should().BeFalse();
    }

    [Fact]
    public async Task IsDirtyAsync_WithNewAppointment_ReturnsTrue()
    {
        await _client.PostAsJsonAsync("/appointments/book/guest", GuestRequest());

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ClinicBookingDbContext>();

        var isDirty = await DemoResetService.IsDirtyAsync(db);

        isDirty.Should().BeTrue();
    }

    [Fact]
    public async Task IsDirtyAsync_WithSoftDeletedAppointment_ReturnsTrue()
    {
        // Book, then soft-delete directly via the DbContext (bypassing the API,
        // which only lets a logged-in registered patient cancel their own
        // appointment - guest bookings can't be cancelled through the API at
        // all). db.Remove() on an ISoftDeletable entity is intercepted by
        // ClinicBookingDbContext.SaveChangesAsync and turned into an UPDATE
        // IsDeleted = 1, not a real DELETE - so the row still exists, just
        // hidden from normal queries by the global query filter.
        await _client.PostAsJsonAsync("/appointments/book/guest", GuestRequest());

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ClinicBookingDbContext>();
            var appointment = await db.Appointments.IgnoreQueryFilters().FirstAsync();
            db.Appointments.Remove(appointment);
            await db.SaveChangesAsync();
        }

        using var checkScope = factory.Services.CreateScope();
        var checkDb = checkScope.ServiceProvider.GetRequiredService<ClinicBookingDbContext>();

        // A normal (filtered) query sees nothing...
        (await checkDb.Appointments.AnyAsync()).Should().BeFalse();
        // ...but IsDirtyAsync must still catch it, via IgnoreQueryFilters().
        (await DemoResetService.IsDirtyAsync(checkDb)).Should().BeTrue();
    }

    [Fact]
    public async Task IsDirtyAsync_WithEditedClinicName_ReturnsTrue()
    {
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ClinicBookingDbContext>();
            var clinic = await db.Clinics.FirstAsync();
            clinic.Name = "Renamed by an admin during a demo";
            await db.SaveChangesAsync();
        }

        using var checkScope = factory.Services.CreateScope();
        var checkDb = checkScope.ServiceProvider.GetRequiredService<ClinicBookingDbContext>();

        // Same row count as the seed, but different content - this only gets
        // caught because IsDirtyAsync compares field values, not just counts.
        (await DemoResetService.IsDirtyAsync(checkDb)).Should().BeTrue();
    }

    // -------------------------------------------------------------------------
    // ResetAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task ResetAsync_OnDirtyDatabase_ClearsTransactionalDataAndRestoresCatalog()
    {
        await _client.PostAsJsonAsync("/appointments/book/guest", GuestRequest());

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ClinicBookingDbContext>();
            var clinic = await db.Clinics.FirstAsync(c => c.Name == "City Medical Center");
            clinic.Name = "Renamed by an admin during a demo";
            await db.SaveChangesAsync();
        }

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ClinicBookingDbContext>();
            await DemoResetService.ResetAsync(db);
        }

        using var checkScope = factory.Services.CreateScope();
        var checkDb = checkScope.ServiceProvider.GetRequiredService<ClinicBookingDbContext>();

        (await checkDb.Patients.AnyAsync()).Should().BeFalse();
        (await checkDb.Appointments.AnyAsync()).Should().BeFalse();
        (await checkDb.RefreshTokens.AnyAsync()).Should().BeFalse();

        // Asserted by content, not by Id - ResetAsync re-inserts catalog rows
        // through EF navigation properties, so they get fresh identity values.
        var clinicNames = await checkDb.Clinics.OrderBy(c => c.Id).Select(c => c.Name).ToListAsync();
        clinicNames.Should().BeEquivalentTo(["City Medical Center", "Westside Health Clinic"]);
    }

    [Fact]
    public async Task ResetAsync_OnCleanDatabase_IsNoOpAndPreservesIds()
    {
        // The database is already pristine right after InitializeAsync's
        // ResetDatabase(), so both calls below should be no-ops. If ResetAsync
        // ever re-seeds when it shouldn't, the catalog row's Id would change -
        // a fresh Add always gets a new identity value - which is exactly what
        // this test would catch.
        int idBefore, idAfter;

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ClinicBookingDbContext>();
            await DemoResetService.ResetAsync(db);
            idBefore = (await db.Specialities.FirstAsync(s => s.Name == "General Practice")).Id;
        }

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ClinicBookingDbContext>();
            await DemoResetService.ResetAsync(db);
            idAfter = (await db.Specialities.FirstAsync(s => s.Name == "General Practice")).Id;
        }

        idAfter.Should().Be(idBefore);
    }

    [Fact]
    public async Task ResetAsync_PreservesAdminAccount()
    {
        int adminIdBefore;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ClinicBookingDbContext>();
            adminIdBefore = (await db.Admins.FirstAsync()).Id;
        }

        await _client.PostAsJsonAsync("/appointments/book/guest", GuestRequest());

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ClinicBookingDbContext>();
            await DemoResetService.ResetAsync(db);
        }

        using var checkScope = factory.Services.CreateScope();
        var checkDb = checkScope.ServiceProvider.GetRequiredService<ClinicBookingDbContext>();
        var admin = await checkDb.Admins.SingleAsync();

        admin.Id.Should().Be(adminIdBefore);
        admin.Email.Should().Be("admin@clinicbook.com");
    }

    [Fact]
    public async Task ResetAsync_HardDeletesSoftDeletedRows()
    {
        await _client.PostAsJsonAsync("/appointments/book/guest", GuestRequest());

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ClinicBookingDbContext>();
            var appointment = await db.Appointments.IgnoreQueryFilters().FirstAsync();
            db.Appointments.Remove(appointment); // soft delete
            await db.SaveChangesAsync();

            await DemoResetService.ResetAsync(db);
        }

        using var checkScope = factory.Services.CreateScope();
        var checkDb = checkScope.ServiceProvider.GetRequiredService<ClinicBookingDbContext>();

        // Not just invisible to the query filter - actually gone. ResetAsync
        // uses raw SQL DELETE, which bypasses the soft-delete override
        // entirely, so no IsDeleted = 1 row is left behind to pile up.
        (await checkDb.Appointments.IgnoreQueryFilters().AnyAsync()).Should().BeFalse();
    }

    private static GuestBookAppointmentRequest GuestRequest() => new()
    {
        FirstName = "Demo",
        LastName = "Visitor",
        Email = $"demo.{Guid.NewGuid()}@example.com",
        Birthdate = new DateTime(1990, 1, 1),
        Gender = "Female",
        DoctorId = 1,
        ClinicId = 1,
        CategoryId = 1,
        StartTime = new DateTime(2030, 1, 1, 9, 0, 0),
        EndTime = new DateTime(2030, 1, 1, 10, 0, 0)
    };
}
