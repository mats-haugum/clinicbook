namespace ClinicAppointmentBookingSystem.Models.DTOs.Appointments;

public class AppointmentResponse
{
    public int Id { get; set; }
    public int DoctorId { get; set; }
    public int ClinicId { get; set; }
    public int CategoryId { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public required string DoctorFullName { get; set; }
    public required string ClinicName { get; set; }
    public required string CategoryName { get; set; }
}
