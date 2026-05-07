using ClinicAppointmentBookingSystem.Models.DTOs.Appointments;

namespace ClinicAppointmentBookingSystem.Services;

public interface IAppointmentService
{
    Task<AppointmentResponse> BookAsGuestAsync(GuestBookAppointmentRequest request);
    Task<AppointmentResponse> BookAsPatientAsync(int patientId, BookAppointmentRequest request);
    Task<List<AppointmentResponse>> GetPatientAppointmentsAsync(int patientId);
    Task<AppointmentResponse> RescheduleAsync(int appointmentId, int patientId, RescheduleAppointmentRequest request);
    Task CancelAsync(int appointmentId, int patientId);
}
