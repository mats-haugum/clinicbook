using ClinicAppointmentBookingSystem.Models.DTOs.Auth;

namespace ClinicAppointmentBookingSystem.Services;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request);
    Task<AuthResponse> LoginAsync(LoginRequest request);
}
