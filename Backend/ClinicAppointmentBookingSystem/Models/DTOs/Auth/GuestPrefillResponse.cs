namespace ClinicAppointmentBookingSystem.Models.DTOs.Auth;

/// <summary>Non-sensitive PII returned when pre-filling the registration form for an existing guest.</summary>
public class GuestPrefillResponse
{
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required string Email { get; set; }
    public DateTime Birthdate { get; set; }
    public required string Gender { get; set; }
}
