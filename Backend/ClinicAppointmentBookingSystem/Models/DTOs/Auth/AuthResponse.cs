namespace ClinicAppointmentBookingSystem.Models.DTOs.Auth;

public class AuthResponse
{
    // Short-lived JWT access token — used in the Authorization header
    public required string Token { get; set; }

    // Long-lived refresh token — used to obtain a new access token when the current one expires
    public required string RefreshToken { get; set; }

    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required string Email { get; set; }
}
