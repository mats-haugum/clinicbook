using ClinicAppointmentBookingSystem.Data;
using ClinicAppointmentBookingSystem.Models.DTOs.Doctors;
using ClinicAppointmentBookingSystem.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace ClinicAppointmentBookingSystem.Services;

public class DoctorService(ClinicBookingDbContext db) : IDoctorService
{
    public async Task<List<DoctorResponse>> GetAllAsync() =>
        await db.Doctors
            .Include(d => d.Speciality)
            .Include(d => d.DoctorClinics)
                .ThenInclude(dc => dc.Clinic)
            .Select(d => new DoctorResponse
            {
                Id = d.Id,
                FirstName = d.FirstName,
                LastName = d.LastName,
                SpecialityName = d.Speciality.Name,
                ClinicNames = d.DoctorClinics.Select(dc => dc.Clinic.Name).ToList()
            })
            .ToListAsync();

    public async Task<DoctorResponse?> GetByIdAsync(int id) =>
        await db.Doctors
            .Include(d => d.Speciality)
            .Include(d => d.DoctorClinics)
                .ThenInclude(dc => dc.Clinic)
            .Where(d => d.Id == id)
            .Select(d => new DoctorResponse
            {
                Id = d.Id,
                FirstName = d.FirstName,
                LastName = d.LastName,
                SpecialityName = d.Speciality.Name,
                ClinicNames = d.DoctorClinics.Select(dc => dc.Clinic.Name).ToList()
            })
            .FirstOrDefaultAsync();

    public async Task<List<DoctorSearchResponse>> SearchAsync(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Search term cannot be empty.");

        return await db.Doctors
            .Include(d => d.Speciality)
            .Include(d => d.DoctorClinics)
                .ThenInclude(dc => dc.Clinic)
            .Where(d => d.FirstName.Contains(name) || d.LastName.Contains(name))
            .SelectMany(d => d.DoctorClinics, (doctor, dc) => new DoctorSearchResponse
            {
                FullName = $"{doctor.FirstName} {doctor.LastName}",
                ClinicName = dc.Clinic.Name,
                Speciality = doctor.Speciality.Name
            })
            .ToListAsync();
    }

    public async Task<DoctorResponse> CreateAsync(CreateDoctorRequest request)
    {
        if (!await db.Specialities.AnyAsync(s => s.Id == request.SpecialityId))
            throw new KeyNotFoundException($"Speciality {request.SpecialityId} not found.");

        var invalidClinicIds = request.ClinicIds
            .Except(await db.Clinics.Select(c => c.Id).ToListAsync())
            .ToList();

        if (invalidClinicIds.Count != 0)
            throw new KeyNotFoundException($"Clinic(s) not found: {string.Join(", ", invalidClinicIds)}.");

        var doctor = new Doctor
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            SpecialityId = request.SpecialityId,
            DoctorClinics = request.ClinicIds
                .Select(clinicId => new DoctorClinic { ClinicId = clinicId })
                .ToList()
        };

        db.Doctors.Add(doctor);
        await db.SaveChangesAsync();

        return (await GetByIdAsync(doctor.Id))!;
    }

    public async Task<DoctorResponse> UpdateAsync(int id, UpdateDoctorRequest request)
    {
        var doctor = await db.Doctors.FindAsync(id)
            ?? throw new KeyNotFoundException($"Doctor {id} not found.");

        if (!await db.Specialities.AnyAsync(s => s.Id == request.SpecialityId))
            throw new KeyNotFoundException($"Speciality {request.SpecialityId} not found.");

        doctor.FirstName = request.FirstName;
        doctor.LastName = request.LastName;
        doctor.SpecialityId = request.SpecialityId;
        await db.SaveChangesAsync();

        return (await GetByIdAsync(id))!;
    }

    public async Task DeleteAsync(int id)
    {
        var doctor = await db.Doctors
            .Include(d => d.DoctorClinics)
            .FirstOrDefaultAsync(d => d.Id == id)
            ?? throw new KeyNotFoundException($"Doctor {id} not found.");

        if (await db.Appointments.AnyAsync(a => a.DoctorId == id))
            throw new InvalidOperationException("Cannot delete a doctor that has appointments assigned to them.");

        db.DoctorClinics.RemoveRange(doctor.DoctorClinics);
        db.Doctors.Remove(doctor);
        await db.SaveChangesAsync();
    }
}
