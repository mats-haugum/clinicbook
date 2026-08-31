using ClinicAppointmentBookingSystem.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace ClinicAppointmentBookingSystem.Data;

// Restores the database to its freshly-seeded state for live demos, so a
// showcase never shows appointments/patients left over from a previous
// visitor, or catalog edits made from the admin panel.
//
// This is meant to be run periodically via `dotnet ... --reset-demo` (see
// Program.cs), not as part of normal request handling.
public static class DemoResetService
{
    // Checks whether the database still matches its pristine seeded state.
    // Called before ResetAsync so an already-clean database (nobody has used
    // the demo since the last reset) is left alone instead of being rewritten
    // for no reason.
    public static async Task<bool> IsDirtyAsync(ClinicBookingDbContext db)
    {
        // IgnoreQueryFilters() is needed here because Appointment/Patient have
        // a global query filter hiding soft-deleted rows (see
        // ClinicBookingDbContext.OnModelCreating) - a soft-deleted row is
        // still a row that needs clearing, even though normal queries can't
        // see it.
        if (await db.Patients.IgnoreQueryFilters().AnyAsync()) return true;
        if (await db.Appointments.IgnoreQueryFilters().AnyAsync()) return true;
        if (await db.RefreshTokens.AnyAsync()) return true;

        var specialities = await db.Specialities.IgnoreQueryFilters()
            .OrderBy(s => s.Id).Select(s => s.Name).ToListAsync();
        if (!specialities.SequenceEqual(SeedSpecialityNames)) return true;

        var clinics = await db.Clinics.IgnoreQueryFilters()
            .OrderBy(c => c.Id).Select(c => new { c.Name, c.Address }).ToListAsync();
        if (clinics.Count != SeedClinics.Length) return true;
        for (var i = 0; i < clinics.Count; i++)
            if (clinics[i].Name != SeedClinics[i].Name || clinics[i].Address != SeedClinics[i].Address)
                return true;

        var doctors = await db.Doctors.IgnoreQueryFilters()
            .OrderBy(d => d.Id)
            .Select(d => new { d.FirstName, d.LastName, SpecialityName = d.Speciality.Name })
            .ToListAsync();
        if (doctors.Count != SeedDoctors.Length) return true;
        for (var i = 0; i < doctors.Count; i++)
            if (doctors[i].FirstName != SeedDoctors[i].FirstName
                || doctors[i].LastName != SeedDoctors[i].LastName
                || doctors[i].SpecialityName != SeedDoctors[i].SpecialityName)
                return true;

        var doctorClinicCount = await db.DoctorClinics.CountAsync();
        if (doctorClinicCount != SeedDoctorClinicLinks.Length) return true;

        var categories = await db.AppointmentCategories.IgnoreQueryFilters()
            .OrderBy(c => c.Id).Select(c => c.Name).ToListAsync();
        if (!categories.SequenceEqual(SeedCategoryNames)) return true;

        return false;
    }

    public static async Task ResetAsync(ClinicBookingDbContext db)
    {
        if (!await IsDirtyAsync(db))
        {
            Console.WriteLine("Demo reset skipped - database already matches the seeded state.");
            return;
        }

        // Raw SQL DELETE bypasses ChangeTracker entirely, so it does a real
        // hard delete instead of going through SaveChangesAsync's soft-delete
        // override. Order matters: children before the parents they
        // reference, or SQL Server rejects the delete with a FK violation.
        await db.Database.ExecuteSqlRawAsync("DELETE FROM Appointments");
        await db.Database.ExecuteSqlRawAsync("DELETE FROM RefreshTokens");
        await db.Database.ExecuteSqlRawAsync("DELETE FROM DoctorClinics");
        await db.Database.ExecuteSqlRawAsync("DELETE FROM Patients");
        await db.Database.ExecuteSqlRawAsync("DELETE FROM Doctors");
        await db.Database.ExecuteSqlRawAsync("DELETE FROM Clinics");
        await db.Database.ExecuteSqlRawAsync("DELETE FROM Specialities");
        await db.Database.ExecuteSqlRawAsync("DELETE FROM AppointmentCategories");

        // Re-insert the catalog data through navigation properties instead of
        // hardcoded Ids. EF Core lets the identity columns assign fresh Ids
        // and fixes up every foreign key itself from the object references
        // below - so this never has to fight SQL Server's IDENTITY_INSERT
        // restriction the way inserting explicit Ids would.
        var generalPractice = new Speciality { Name = "General Practice" };
        var cardiology = new Speciality { Name = "Cardiology" };
        var dermatology = new Speciality { Name = "Dermatology" };
        db.Specialities.AddRange(generalPractice, cardiology, dermatology);

        var cityMedical = new Clinic { Name = "City Medical Center", Address = "123 Main Street, Cape Town" };
        var westside = new Clinic { Name = "Westside Health Clinic", Address = "456 Oak Avenue, Johannesburg" };
        db.Clinics.AddRange(cityMedical, westside);

        var james = new Doctor { FirstName = "James", LastName = "Wilson", Speciality = generalPractice };
        var sarah = new Doctor { FirstName = "Sarah", LastName = "Connor", Speciality = cardiology };
        var emily = new Doctor { FirstName = "Emily", LastName = "Chen", Speciality = dermatology };
        var michael = new Doctor { FirstName = "Michael", LastName = "Brown", Speciality = generalPractice };
        db.Doctors.AddRange(james, sarah, emily, michael);

        db.DoctorClinics.AddRange(
            new DoctorClinic { Doctor = james, Clinic = cityMedical },
            new DoctorClinic { Doctor = james, Clinic = westside },
            new DoctorClinic { Doctor = sarah, Clinic = cityMedical },
            new DoctorClinic { Doctor = emily, Clinic = westside },
            new DoctorClinic { Doctor = michael, Clinic = cityMedical }
        );

        db.AppointmentCategories.AddRange(
            SeedCategoryNames.Select(name => new AppointmentCategory { Name = name })
        );

        await db.SaveChangesAsync();
        Console.WriteLine("Demo reset complete.");
    }

    // The values below must match ClinicBookingDbContext.OnModelCreating's
    // HasData seed exactly - that's what "the pristine seeded state" means.
    private static readonly string[] SeedSpecialityNames =
        ["General Practice", "Cardiology", "Dermatology"];

    private static readonly (string Name, string Address)[] SeedClinics =
    [
        ("City Medical Center", "123 Main Street, Cape Town"),
        ("Westside Health Clinic", "456 Oak Avenue, Johannesburg"),
    ];

    private static readonly (string FirstName, string LastName, string SpecialityName)[] SeedDoctors =
    [
        ("James", "Wilson", "General Practice"),
        ("Sarah", "Connor", "Cardiology"),
        ("Emily", "Chen", "Dermatology"),
        ("Michael", "Brown", "General Practice"),
    ];

    // Only the count is checked for these - (doctor index, clinic index) pairs
    // matching the DoctorId/ClinicId link data in OnModelCreating.
    private static readonly (int DoctorIndex, int ClinicIndex)[] SeedDoctorClinicLinks =
        [(0, 0), (0, 1), (1, 0), (2, 1), (3, 0)];

    private static readonly string[] SeedCategoryNames =
        ["General Checkup", "Follow-up", "Specialist Consultation", "Urgent Care"];
}
