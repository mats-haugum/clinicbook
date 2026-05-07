namespace ClinicAppointmentBookingSystem.Models.Entities;

public class Appointment
{
    public int Id { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }

    public int PatientId { get; set; }
    public Patient Patient { get; set; } = null!;

    public int DoctorId { get; set; }
    public Doctor Doctor { get; set; } = null!;

    public int ClinicId { get; set; }
    public Clinic Clinic { get; set; } = null!;

    public int CategoryId { get; set; }
    public AppointmentCategory Category { get; set; } = null!;
}
