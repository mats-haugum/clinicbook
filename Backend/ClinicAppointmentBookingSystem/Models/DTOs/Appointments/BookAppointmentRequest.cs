namespace ClinicAppointmentBookingSystem.Models.DTOs.Appointments;

public class BookAppointmentRequest
{
    public int DoctorId { get; set; }
    public int ClinicId { get; set; }
    public int CategoryId { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
}
