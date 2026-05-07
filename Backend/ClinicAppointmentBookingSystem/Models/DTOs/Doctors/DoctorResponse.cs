namespace ClinicAppointmentBookingSystem.Models.DTOs.Doctors;

public class DoctorResponse
{
    public int Id { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required string SpecialityName { get; set; }
    public List<string> ClinicNames { get; set; } = [];
}
