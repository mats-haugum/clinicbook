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
        await ValidateAppointmentSlotAsync(request.ClinicId, request.DoctorId, request.StartTime, request.EndTime);

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
        await ValidateAppointmentSlotAsync(request.ClinicId, request.DoctorId, request.StartTime, request.EndTime, patientId);

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
                StartTime = a.StartTime,
                EndTime = a.EndTime,
                DoctorFullName = $"{a.Doctor.FirstName} {a.Doctor.LastName}",
                ClinicName = a.Clinic.Name,
                CategoryName = a.Category.Name
            })
            .ToListAsync();

    public async Task<AppointmentResponse> RescheduleAsync(int appointmentId, int patientId, RescheduleAppointmentRequest request)
    {
        var appointment = await db.Appointments.FindAsync(appointmentId)
            ?? throw new KeyNotFoundException($"Appointment {appointmentId} not found.");

        if (appointment.PatientId != patientId)
            throw new UnauthorizedAccessException("You can only reschedule your own appointments.");

        await ValidateAppointmentSlotAsync(appointment.ClinicId, appointment.DoctorId, request.StartTime, request.EndTime, patientId, excludeAppointmentId: appointmentId);

        appointment.StartTime = request.StartTime;
        appointment.EndTime = request.EndTime;
        await db.SaveChangesAsync();

        return await ToResponseAsync(appointment.Id);
    }

    public async Task CancelAsync(int appointmentId, int patientId)
    {
        var appointment = await db.Appointments.FindAsync(appointmentId)
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

    private async Task ValidateAppointmentSlotAsync(int clinicId, int doctorId, DateTime start, DateTime end, int? patientId = null, int? excludeAppointmentId = null)
    {
        if (end <= start)
            throw new ArgumentException("End time must be after start time.");

        var query = db.Appointments.Where(a =>
            a.StartTime < end && a.EndTime > start &&
            a.ClinicId == clinicId);

        if (excludeAppointmentId.HasValue)
            query = query.Where(a => a.Id != excludeAppointmentId.Value);

        if (patientId.HasValue && await query.AnyAsync(a => a.PatientId == patientId.Value))
            throw new InvalidOperationException("You already have an appointment at this clinic during this time slot.");

        if (await query.AnyAsync(a => a.DoctorId == doctorId))
            throw new InvalidOperationException("The doctor already has an appointment during this time slot.");
    }

    private async Task<AppointmentResponse> ToResponseAsync(int appointmentId) =>
        await db.Appointments
            .Where(a => a.Id == appointmentId)
            .Select(a => new AppointmentResponse
            {
                Id = a.Id,
                StartTime = a.StartTime,
                EndTime = a.EndTime,
                DoctorFullName = $"{a.Doctor.FirstName} {a.Doctor.LastName}",
                ClinicName = a.Clinic.Name,
                CategoryName = a.Category.Name
            })
            .FirstAsync();
}
