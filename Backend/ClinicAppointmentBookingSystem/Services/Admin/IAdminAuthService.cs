using ClinicAppointmentBookingSystem.Models.DTOs.Admin;

namespace ClinicAppointmentBookingSystem.Services.Admin;

public interface IAdminAuthService
{
    Task<AdminAuthResponse> LoginAsync(AdminLoginRequest request);
}
