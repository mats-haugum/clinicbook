namespace ClinicAppointmentBookingSystem.Models.DTOs.Admin;

// Admins receive only an access token — no refresh token.
// Admin sessions are intentionally shorter-lived; re-authentication is required when the token expires.
public class AdminAuthResponse
{
    public required string Token { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required string Email { get; set; }
}
