using ClinicAppointmentBookingSystem.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace ClinicAppointmentBookingSystem.Data;

public class ClinicBookingDbContext(DbContextOptions<ClinicBookingDbContext> options) : DbContext(options)
{
    public DbSet<Speciality> Specialities { get; set; }
    public DbSet<Clinic> Clinics { get; set; }
    public DbSet<Doctor> Doctors { get; set; }
    public DbSet<DoctorClinic> DoctorClinics { get; set; }
    public DbSet<AppointmentCategory> AppointmentCategories { get; set; }
    public DbSet<Patient> Patients { get; set; }
    public DbSet<Appointment> Appointments { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DoctorClinic>()
            .HasKey(dc => new { dc.DoctorId, dc.ClinicId });

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
