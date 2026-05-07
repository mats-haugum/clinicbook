namespace ClinicAppointmentBookingSystem.Models.DTOs.Clinics;

public class ClinicResponse
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string Address { get; set; }
}
