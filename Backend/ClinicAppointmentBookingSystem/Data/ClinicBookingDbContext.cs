using ClinicAppointmentBookingSystem.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace ClinicAppointmentBookingSystem.Data;

public class ClinicBookingDbContext(DbContextOptions<ClinicBookingDbContext> options) : DbContext(options)
{
    // One DbSet per table — the entry point for all LINQ queries against that table
    public DbSet<Speciality> Specialities { get; set; }
    public DbSet<Clinic> Clinics { get; set; }
    public DbSet<Doctor> Doctors { get; set; }
    public DbSet<DoctorClinic> DoctorClinics { get; set; }
    public DbSet<AppointmentCategory> AppointmentCategories { get; set; }
    public DbSet<Patient> Patients { get; set; }
    public DbSet<Appointment> Appointments { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    public DbSet<Admin> Admins { get; set; }

    // Intercepts every db.Remove() call on a soft-deletable entity.
    // Instead of issuing a DELETE statement, EF issues an UPDATE that sets IsDeleted = true.
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<ISoftDeletable>()
            .Where(e => e.State == EntityState.Deleted))
        {
            // Flip state to Modified so EF generates UPDATE instead of DELETE
            entry.State = EntityState.Modified;
            entry.Entity.IsDeleted = true;
            entry.Entity.DeletedAt = DateTime.UtcNow;
        }

        return await base.SaveChangesAsync(cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DoctorClinic>()
            .HasKey(dc => new { dc.DoctorId, dc.ClinicId });

        // Global query filters — EF automatically appends WHERE IsDeleted = 0 to every
        // query on these entities, so soft-deleted records are invisible to all code.
        modelBuilder.Entity<Appointment>().HasQueryFilter(a => !a.IsDeleted);
        modelBuilder.Entity<Patient>().HasQueryFilter(p => !p.IsDeleted);
        modelBuilder.Entity<Doctor>().HasQueryFilter(d => !d.IsDeleted);
        modelBuilder.Entity<Clinic>().HasQueryFilter(c => !c.IsDeleted);
        modelBuilder.Entity<Speciality>().HasQueryFilter(s => !s.IsDeleted);
        modelBuilder.Entity<AppointmentCategory>().HasQueryFilter(ac => !ac.IsDeleted);

        // Disable cascade delete on all required FK relationships.
        // We soft-delete parents instead of hard-deleting them, so the FK constraint
        // is never violated and Restrict (= NO ACTION in SQL) is safe here.
        modelBuilder.Entity<Appointment>()
            .HasOne(a => a.Patient).WithMany(p => p.Appointments)
            .HasForeignKey(a => a.PatientId).OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Appointment>()
            .HasOne(a => a.Doctor).WithMany(d => d.Appointments)
            .HasForeignKey(a => a.DoctorId).OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Appointment>()
            .HasOne(a => a.Clinic).WithMany(c => c.Appointments)
            .HasForeignKey(a => a.ClinicId).OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Appointment>()
            .HasOne(a => a.Category).WithMany(ac => ac.Appointments)
            .HasForeignKey(a => a.CategoryId).OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Doctor>()
            .HasOne(d => d.Speciality).WithMany(s => s.Doctors)
            .HasForeignKey(d => d.SpecialityId).OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<RefreshToken>()
            .HasOne(rt => rt.Patient).WithMany(p => p.RefreshTokens)
            .HasForeignKey(rt => rt.PatientId).OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<DoctorClinic>()
            .HasOne(dc => dc.Doctor).WithMany(d => d.DoctorClinics)
            .HasForeignKey(dc => dc.DoctorId).OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<DoctorClinic>()
            .HasOne(dc => dc.Clinic).WithMany(c => c.DoctorClinics)
            .HasForeignKey(dc => dc.ClinicId).OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Speciality>().HasData(
            new Speciality { Id = 1, Name = "General Practice" },
            new Speciality { Id = 2, Name = "Cardiology" },
            new Speciality { Id = 3, Name = "Dermatology" }
        );

        modelBuilder.Entity<Clinic>().HasData(
            new Clinic { Id = 1, Name = "City Medical Center",   Address = "123 Main Street, Cape Town" },
            new Clinic { Id = 2, Name = "Westside Health Clinic", Address = "456 Oak Avenue, Johannesburg" }
        );

        modelBuilder.Entity<Doctor>().HasData(
            new Doctor { Id = 1, FirstName = "James",   LastName = "Wilson",  SpecialityId = 1 },
            new Doctor { Id = 2, FirstName = "Sarah",   LastName = "Connor",  SpecialityId = 2 },
            new Doctor { Id = 3, FirstName = "Emily",   LastName = "Chen",    SpecialityId = 3 },
            new Doctor { Id = 4, FirstName = "Michael", LastName = "Brown",   SpecialityId = 1 }
        );

        modelBuilder.Entity<DoctorClinic>().HasData(
            new DoctorClinic { DoctorId = 1, ClinicId = 1 },
            new DoctorClinic { DoctorId = 1, ClinicId = 2 },
            new DoctorClinic { DoctorId = 2, ClinicId = 1 },
            new DoctorClinic { DoctorId = 3, ClinicId = 2 },
            new DoctorClinic { DoctorId = 4, ClinicId = 1 }
        );

        modelBuilder.Entity<AppointmentCategory>().HasData(
            new AppointmentCategory { Id = 1, Name = "General Checkup" },
            new AppointmentCategory { Id = 2, Name = "Follow-up" },
            new AppointmentCategory { Id = 3, Name = "Specialist Consultation" },
            new AppointmentCategory { Id = 4, Name = "Urgent Care" }
        );
    }
}
