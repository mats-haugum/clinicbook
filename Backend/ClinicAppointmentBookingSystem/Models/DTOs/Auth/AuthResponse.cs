namespace ClinicAppointmentBookingSystem.Models.DTOs.Auth;

public class AuthResponse
{
    public required string Token { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required string Email { get; set; }
}
