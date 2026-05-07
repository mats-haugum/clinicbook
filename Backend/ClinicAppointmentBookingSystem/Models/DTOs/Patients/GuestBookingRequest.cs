namespace ClinicAppointmentBookingSystem.Models.DTOs.Patients;

public class GuestBookingRequest
{
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required string Email { get; set; }
    public DateTime Birthdate { get; set; }
    public required string Gender { get; set; }
}
