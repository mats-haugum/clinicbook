namespace ClinicAppointmentBookingSystem.Models.DTOs.Doctors;

public class DoctorSearchResponse
{
    public required string FullName { get; set; }
    public required string ClinicName { get; set; }
    public required string Speciality { get; set; }
}
