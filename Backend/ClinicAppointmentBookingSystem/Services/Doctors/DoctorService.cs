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

    public async Task<List<DoctorAvailabilitySlot>> GetAvailabilityAsync(int doctorId, DateOnly date)
    {
        if (!await db.Doctors.AnyAsync(d => d.Id == doctorId))
            throw new KeyNotFoundException($"Doctor {doctorId} not found.");

        // Build every 30-minute slot between 08:00 and 17:00 for the given day.
        // DateTime with no Kind specified is treated as wall-clock (local) time,
        // which is consistent with how appointments are stored in this system.
        var slots = new List<DoctorAvailabilitySlot>();
        var slotStart = new DateTime(date.Year, date.Month, date.Day, 8, 0, 0);
        var dayEnd    = new DateTime(date.Year, date.Month, date.Day, 17, 0, 0);

        while (slotStart < dayEnd)
        {
            var slotEnd = slotStart.AddMinutes(30);
            slots.Add(new DoctorAvailabilitySlot { StartTime = slotStart, EndTime = slotEnd, IsAvailable = true });
            slotStart = slotEnd;
        }

        // Fetch all (non-soft-deleted) appointments for this doctor that overlap the day.
        var dayStart      = new DateTime(date.Year, date.Month, date.Day, 0, 0, 0);
        var nextDayStart  = dayStart.AddDays(1);

        var booked = await db.Appointments
            .Where(a => a.DoctorId == doctorId && a.StartTime < nextDayStart && a.EndTime > dayStart)
            .Select(a => new { a.StartTime, a.EndTime })
            .ToListAsync();

        // The same overlap formula used in conflict validation:
        // two intervals overlap when one starts before the other ends AND ends after the other starts.
        foreach (var slot in slots)
        {
            if (booked.Any(a => a.StartTime < slot.EndTime && a.EndTime > slot.StartTime))
                slot.IsAvailable = false;
        }

        return slots;
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
        var doctor = await db.Doctors.FirstOrDefaultAsync(d => d.Id == id)
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
