using ClinicAppointmentBookingSystem.Data;
using ClinicAppointmentBookingSystem.Models.DTOs.Appointments;
using ClinicAppointmentBookingSystem.Models.Entities;
using ClinicAppointmentBookingSystem.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace ClinicAppointmentBookingSystem.Services;

public class AppointmentService(ClinicBookingDbContext db) : IAppointmentService
{
    public async Task<AppointmentResponse> BookAsGuestAsync(GuestBookAppointmentRequest request)
    {
        await ValidateBookingReferencesAsync(request.DoctorId, request.ClinicId, request.CategoryId);
        await ValidateAppointmentSlotAsync(request.DoctorId, request.StartTime, request.EndTime);

        // Prevent a guest from using an email that belongs to a registered patient.
        // Without this check a guest booking would silently create a second patient row
        // with the same address, hiding the real account's appointments.
        if (await db.Patients.AnyAsync(p => p.Email == request.Email && p.UserType == UserType.Patient))
            throw new InvalidOperationException("An account with this email already exists. Please log in to book.");

        // Reuse the existing guest record if one exists for this email so that a
        // returning guest accumulates all their appointments on the same patient row.
        var guest = await db.Patients.FirstOrDefaultAsync(p =>
            p.Email == request.Email && p.UserType == UserType.Guest)
            ?? new Patient
            {
                FirstName = request.FirstName,
                LastName = request.LastName,
                Email = request.Email,
                Birthdate = request.Birthdate,
                Gender = request.Gender,
                UserType = UserType.Guest
            };

        if (guest.Id == 0)
            db.Patients.Add(guest);

        var appointment = new Appointment
        {
            Patient = guest,
            DoctorId = request.DoctorId,
            ClinicId = request.ClinicId,
            CategoryId = request.CategoryId,
            StartTime = request.StartTime,
            EndTime = request.EndTime
        };

        db.Appointments.Add(appointment);
        await db.SaveChangesAsync();

        return await ToResponseAsync(appointment.Id);
    }

    public async Task<AppointmentResponse> BookAsPatientAsync(int patientId, BookAppointmentRequest request)
    {
        await ValidateBookingReferencesAsync(request.DoctorId, request.ClinicId, request.CategoryId);
        await ValidateAppointmentSlotAsync(request.DoctorId, request.StartTime, request.EndTime, patientId);

        var appointment = new Appointment
        {
            PatientId = patientId,
            DoctorId = request.DoctorId,
            ClinicId = request.ClinicId,
            CategoryId = request.CategoryId,
            StartTime = request.StartTime,
            EndTime = request.EndTime
        };

        db.Appointments.Add(appointment);
        await db.SaveChangesAsync();

        return await ToResponseAsync(appointment.Id);
    }

    public async Task<List<AppointmentResponse>> GetPatientAppointmentsAsync(int patientId) =>
        await db.Appointments
            .Where(a => a.PatientId == patientId)
            .Select(a => new AppointmentResponse
            {
                Id = a.Id,
                DoctorId = a.DoctorId,
                ClinicId = a.ClinicId,
                CategoryId = a.CategoryId,
                StartTime = a.StartTime,
                EndTime = a.EndTime,
                DoctorFullName = $"{a.Doctor.FirstName} {a.Doctor.LastName}",
                ClinicName = a.Clinic.Name,
                CategoryName = a.Category.Name
            })
            .ToListAsync();

    public async Task<AppointmentResponse> RescheduleAsync(int appointmentId, int patientId, RescheduleAppointmentRequest request)
    {
        var appointment = await db.Appointments.FirstOrDefaultAsync(a => a.Id == appointmentId)
            ?? throw new KeyNotFoundException($"Appointment {appointmentId} not found.");

        if (appointment.PatientId != patientId)
            throw new UnauthorizedAccessException("You can only reschedule your own appointments.");

        // Validate the new doctor/clinic/category references when they are being changed
        if (request.DoctorId.HasValue && !await db.Doctors.AnyAsync(d => d.Id == request.DoctorId.Value))
            throw new KeyNotFoundException($"Doctor {request.DoctorId} not found.");
        if (request.ClinicId.HasValue && !await db.Clinics.AnyAsync(c => c.Id == request.ClinicId.Value))
            throw new KeyNotFoundException($"Clinic {request.ClinicId} not found.");
        if (request.CategoryId.HasValue && !await db.AppointmentCategories.AnyAsync(c => c.Id == request.CategoryId.Value))
            throw new KeyNotFoundException($"Category {request.CategoryId} not found.");

        // Use the new doctor for conflict checking if a different doctor was chosen
        var effectiveDoctorId = request.DoctorId ?? appointment.DoctorId;
        await ValidateAppointmentSlotAsync(effectiveDoctorId, request.StartTime, request.EndTime, patientId, excludeAppointmentId: appointmentId);

        appointment.StartTime = request.StartTime;
        appointment.EndTime = request.EndTime;
        if (request.DoctorId.HasValue)   appointment.DoctorId   = request.DoctorId.Value;
        if (request.ClinicId.HasValue)   appointment.ClinicId   = request.ClinicId.Value;
        if (request.CategoryId.HasValue) appointment.CategoryId = request.CategoryId.Value;
        await db.SaveChangesAsync();

        return await ToResponseAsync(appointment.Id);
    }

    public async Task CancelAsync(int appointmentId, int patientId)
    {
        var appointment = await db.Appointments.FirstOrDefaultAsync(a => a.Id == appointmentId)
            ?? throw new KeyNotFoundException($"Appointment {appointmentId} not found.");

        if (appointment.PatientId != patientId)
            throw new UnauthorizedAccessException("You can only cancel your own appointments.");

        db.Appointments.Remove(appointment);
        await db.SaveChangesAsync();
    }

    private async Task ValidateBookingReferencesAsync(int doctorId, int clinicId, int categoryId)
    {
        if (!await db.Doctors.AnyAsync(d => d.Id == doctorId))
            throw new KeyNotFoundException($"Doctor {doctorId} not found.");
        if (!await db.Clinics.AnyAsync(c => c.Id == clinicId))
            throw new KeyNotFoundException($"Clinic {clinicId} not found.");
        if (!await db.AppointmentCategories.AnyAsync(c => c.Id == categoryId))
            throw new KeyNotFoundException($"Category {categoryId} not found.");
    }

    // Working hours: appointments must fall entirely within 08:00–17:00 on the same day.
    private static readonly TimeOnly WorkdayStart = new(8, 0);
    private static readonly TimeOnly WorkdayEnd   = new(17, 0);

    private async Task ValidateAppointmentSlotAsync(int doctorId, DateTime start, DateTime end, int? patientId = null, int? excludeAppointmentId = null)
    {
        if (end <= start)
            throw new ArgumentException("End time must be after start time.");

        if (start.Date != end.Date)
            throw new ArgumentException("An appointment cannot span multiple days.");

        if (TimeOnly.FromDateTime(start) < WorkdayStart || TimeOnly.FromDateTime(end) > WorkdayEnd)
            throw new ArgumentException("Appointments must be within working hours (08:00–17:00).");

        // Base query: any appointment that overlaps the requested time window.
        // ClinicId is intentionally NOT filtered here — a person can only be in
        // one place at a time, so conflicts must be checked across all clinics.
        var query = db.Appointments.Where(a =>
            a.StartTime < end && a.EndTime > start);

        if (excludeAppointmentId.HasValue)
            query = query.Where(a => a.Id != excludeAppointmentId.Value);

        // Patient conflict: the same patient cannot have overlapping appointments anywhere
        if (patientId.HasValue && await query.AnyAsync(a => a.PatientId == patientId.Value))
            throw new InvalidOperationException("You already have an appointment during this time slot.");

        // Doctor conflict: the same doctor cannot have overlapping appointments anywhere
        if (await query.AnyAsync(a => a.DoctorId == doctorId))
            throw new InvalidOperationException("The doctor already has an appointment during this time slot.");
    }

    private async Task<AppointmentResponse> ToResponseAsync(int appointmentId) =>
        await db.Appointments
            .Where(a => a.Id == appointmentId)
            .Select(a => new AppointmentResponse
            {
                Id = a.Id,
                DoctorId = a.DoctorId,
                ClinicId = a.ClinicId,
                CategoryId = a.CategoryId,
                StartTime = a.StartTime,
                EndTime = a.EndTime,
                DoctorFullName = $"{a.Doctor.FirstName} {a.Doctor.LastName}",
                ClinicName = a.Clinic.Name,
                CategoryName = a.Category.Name
            })
            .FirstAsync();
}
